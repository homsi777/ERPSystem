#!/usr/bin/env python3
"""Safely instrument WPF load methods with ScreenLoadProfiler."""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
USING = "using ERPSystem.Diagnostics.Performance;"

TARGETS: list[tuple[str, str, str]] = [
    ("Controls/Customers/CustomerListPageControl.cs", "Customers.List", "LoadCustomersAsync"),
    ("Controls/Suppliers/SupplierListPageControl.cs", "Suppliers.List", "LoadAsync"),
    ("Controls/Purchases/PurchaseInvoiceListPageControl.cs", "Purchases.Invoices", "LoadAsync"),
    ("Controls/Sales/SalesReturnListPageControl.cs", "Sales.Returns", "ReloadAsync"),
    ("Controls/Sales/SalesDeliveryListPageControl.cs", "Sales.Delivery", "ReloadAsync"),
    ("Controls/Finance/CashboxListPageControl.cs", "Finance.Cashboxes", "LoadAsync"),
    ("Controls/Finance/CashboxTransferListPageControl.cs", "Finance.Transfers", "LoadAsync"),
    ("Controls/Finance/OpeningBalanceListPageControl.cs", "Finance.OpeningBalances", "LoadAsync"),
    ("Controls/Inventory/InventoryWarehouseListPageControl.cs", "Inventory.Warehouses", "LoadAsync"),
    ("Controls/Inventory/InventoryFabricCategoriesPageControl.cs", "Inventory.Categories", "LoadAsync"),
    ("Controls/China/ContainerListPageControl.cs", "China.Containers", "LoadContainersAsync"),
    ("Controls/Expenses/ExpenseListPageControl.cs", "Expenses.List", "LoadAsync"),
    ("Controls/Expenses/ExpenseEntryListPageControl.cs", "Expenses.Entries", "LoadAsync"),
    ("Controls/Capital/CapitalPartnerListPageControl.cs", "Capital.Partners", "LoadAsync"),
    ("Controls/Capital/CapitalTransactionListPageControl.cs", "Capital.Transactions", "LoadAsync"),
    ("Controls/Hr/EmployeeListPageControl.cs", "HR.Employees", "LoadAsync"),
    ("Controls/Hr/DepartmentListPageControl.cs", "HR.Departments", "LoadAsync"),
    ("Controls/Accounting/JournalEntryListPageControl.cs", "Accounting.Journal", "LoadAsync"),
    ("Controls/Accounting/JournalBookListPageControl.cs", "Accounting.JournalBooks", "LoadAsync"),
    ("Controls/Accounting/ChartOfAccountsListPageControl.cs", "Accounting.Chart", "LoadAsync"),
    ("Controls/Accounting/AgingListControls.cs", "Accounting.ReceivablesAging", "LoadAsync"),
    ("Controls/Sales/SalesTaxReportPageControl.cs", "Sales.TaxReport", "RunAsync"),
    ("Controls/Reports/ModuleReportViewControl.cs", "Reports.ModuleReport", "RunAsync"),
    ("Controls/Accounting/TrialBalanceReportControl.cs", "Accounting.TrialBalance", "LoadAsync"),
    ("Controls/Purchases/PurchaseOrderListPageControl.cs", "Purchases.Orders", "PurchaseOrderListPageControl"),
    ("Controls/Purchases/PurchaseOrderListPageControl.cs", "Purchases.Returns", "PurchaseReturnListPageControl"),
]

OC_TARGETS: list[tuple[str, str, str, str]] = [
    ("Controls/Customers/CustomerOperationsCenterControl.cs", "Customers.OperationsCenter", "OnLoaded", "CustomerUiService.Instance.GetOperationsCenterAsync(_customerId)"),
    ("Controls/Suppliers/SupplierOperationsCenterControl.cs", "Suppliers.OperationsCenter", "OnLoaded", "SupplierUiService.Instance.GetOperationsCenterAsync(_supplierId)"),
    ("Controls/Purchases/PurchaseInvoiceOperationsCenterControl.cs", "Purchases.OperationsCenter", "OnLoaded", "PurchaseUiService.Instance.GetOperationsCenterAsync(_invoiceId)"),
    ("Controls/China/ChinaContainerOperationsCenterControl.cs", "China.OperationsCenter", "ReloadAsync", "ContainerUiService.Instance.GetOperationsCenterAsync(_containerId)"),
    ("Controls/Inventory/InventoryOperationsCenterControl.cs", "Inventory.OperationsCenter", "LoadAsync", "InventoryUiService.Instance.GetOperationsCenterAsync(warehouseId)"),
    ("Controls/Finance/CashboxOperationsCenterControl.cs", "Finance.CashboxOperationsCenter", "LoadAsync", "FinanceUiService.Instance.GetCashboxOperationsCenterAsync(cashboxId)"),
    ("Controls/Expenses/ExpenseOperationsCenterControl.cs", "Expenses.OperationsCenter", "LoadAsync", "ExpenseUiService.Instance.GetOperationsCenterAsync(expenseId)"),
    ("Controls/Capital/CapitalOperationsCenterControl.cs", "Capital.OperationsCenter", "LoadAsync", "CapitalPartnerUiService.Instance.GetOperationsCenterAsync(partnerId)"),
    ("Controls/Finance/OpeningBalanceOperationsCenterControl.cs", "Finance.OpeningBalanceOperationsCenter", "LoadAsync", "OpeningBalanceUiService.Instance.GetDetailsAsync(documentId)"),
]


def ensure_using(text: str) -> str:
    if USING in text:
        return text
    lines = text.splitlines(keepends=True)
    idx = max(i for i, line in enumerate(lines) if line.startswith("using "))
    lines.insert(idx + 1, USING + "\n")
    return "".join(lines)


def extract_method(text: str, method: str) -> tuple[int, int, str] | None:
    if method.endswith("Control"):
        # nested class constructor with Loaded += async
        pat = rf"public sealed class {re.escape(method)}\s*\{{[\s\S]*?Loaded \+= async \(_, _\) =>\s*\{{([\s\S]*?)\}}\;"
        m = re.search(pat, text)
        if not m:
            return None
        return m.start(1), m.end(1), m.group(1)

    pat = rf"private async (?:void|Task(?:<[^>]+>)?) {re.escape(method)}\([^{{]*\)\s*\{{"
    m = re.search(pat, text)
    if not m:
        return None
    start = m.end()
    depth = 1
    i = start
    while i < len(text) and depth:
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
        i += 1
    return start, i - 1, text[start : i - 1]


def wrap_any_result_await(body: str) -> str:
    if "MeasureLoadAsync" in body:
        return body
    m = re.search(r"(\s*)var (\w+) = await (.+?);", body, re.DOTALL)
    if not m:
        return body
    indent = m.group(1)
    var = m.group(2)
    expr = re.sub(r"\s+", " ", m.group(3).strip())
    repl = f"{indent}var {var} = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => {expr});"
    return body[: m.start()] + repl + body[m.end() :]


def wrap_result_await(body: str) -> str:
    return wrap_any_result_await(body)


def insert_begin(body: str, screen: str) -> str:
    if f'ScreenLoadProfiler.Begin("{screen}")' in body:
        return body
    return f'\n        using var perfScope = ScreenLoadProfiler.Begin("{screen}");' + body


def patch_method(text: str, method: str, screen: str) -> str:
    extracted = extract_method(text, method)
    if not extracted:
        raise RuntimeError(f"method/class {method} not found")
    start, end, body = extracted
    body = insert_begin(body, screen)
    body = wrap_result_await(body)
    if "IncrementServiceCalls" not in body and "var result = await ScreenLoadProfiler" in body:
        body = re.sub(
            r"(var result = await ScreenLoadProfiler\.MeasureLoadAsync\(perfScope, \(\) => .+?\);)",
            r"\1\n        perfScope?.IncrementServiceCalls();",
            body,
            count=1,
        )
    return text[:start] + body + text[end:]


def patch_payables(text: str) -> str:
    marker = 'ScreenLoadProfiler.Begin("Accounting.PayablesAging")'
    if marker in text:
        return text
    parts = text.split("public sealed class PayablesAgingControl")
    if len(parts) != 2:
        return text
    head, tail = parts[0], "public sealed class PayablesAgingControl" + parts[1]
    tail = patch_method(tail, "LoadAsync", "Accounting.PayablesAging")
    return head + tail


def patch_oc(text: str, method: str, screen: str, await_expr: str) -> str:
    if f'ScreenLoadProfiler.Begin("{screen}")' in text:
        return text
    extracted = extract_method(text, method)
    if not extracted:
        raise RuntimeError(f"{method} not found in OC")
    start, end, body = extracted
    body = insert_begin(body, screen)
    body = wrap_any_result_await(body)
    if "IncrementServiceCalls" not in body and "MeasureLoadAsync" in body:
        body = re.sub(
            r"(var \w+ = await ScreenLoadProfiler\.MeasureLoadAsync\(perfScope, \(\) => .+?\);)",
            r"\1\n        perfScope?.IncrementServiceCalls();",
            body,
            count=1,
        )
    return text[:start] + body + text[end:]


def main() -> None:
    for rel, screen, method in TARGETS:
        path = ROOT / rel
        text = ensure_using(path.read_text(encoding="utf-8"))
        text = patch_method(text, method, screen)
        if rel.endswith("AgingListControls.cs"):
            text = patch_payables(text)
        path.write_text(text, encoding="utf-8")
        print(f"OK {rel} -> {screen}")

    for rel, screen, method, expr in OC_TARGETS:
        path = ROOT / rel
        text = ensure_using(path.read_text(encoding="utf-8"))
        text = patch_oc(text, method, screen, expr)
        path.write_text(text, encoding="utf-8")
        print(f"OK OC {rel} -> {screen}")


if __name__ == "__main__":
    main()
