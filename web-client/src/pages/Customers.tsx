import { useMemo, useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import {
  getCustomerDetails,
  getCustomerSalesDetails,
  getCustomerStatement,
  getCustomers
} from '../api/customers.ts';
import { ApiError } from '../api/client.ts';
import type { CustomerListDto, CustomerStatus, CustomerType } from '../api/types.ts';
import { AppShell } from '../components/AppShell.tsx';
import { DataCard } from '../components/DataCard.tsx';
import { EmptyState } from '../components/EmptyState.tsx';
import { ErrorState } from '../components/ErrorState.tsx';
import { Icon } from '../components/Icon.tsx';
import { LoadingState } from '../components/LoadingState.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import { formatCurrency, formatDateOnly, formatNumber } from '../lib/format.ts';
import { customerStatusLabels, customerTypeLabels, documentTypeLabels, getCustomerStatusTone } from '../lib/enums.ts';

const PAGE_SIZE = 10;

export function CustomersPage() {
  const { customerId } = useParams();

  if (customerId) {
    return <CustomerDetailsPage customerId={customerId} />;
  }

  return <CustomerListPage />;
}

function CustomerListPage() {
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  const customersQuery = useQuery({
    queryKey: ['customers', search, page],
    queryFn: () => getCustomers({ search: search || undefined, page, pageSize: PAGE_SIZE })
  });

  const pageSummary = useMemo(() => {
    const rows = customersQuery.data?.items ?? [];
    // عرض فقط: مجموع صفحة النتائج الحالية، وليس إجمالي كل العملاء.
    return {
      count: rows.length,
      outstanding: rows.reduce((sum, row) => sum + row.balance, 0)
    };
  }, [customersQuery.data?.items]);

  const headerSummary = (
    <>
      <SummaryCard label="عملاء هذه الصفحة" value={formatNumber(pageSummary.count)} />
      <SummaryCard label="إجمالي الأرصدة (الصفحة)" value={formatCurrency(pageSummary.outstanding)} tone="amber" />
    </>
  );

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSearch(searchInput.trim());
    setPage(1);
  }

  return (
    <AppShell title="العملاء" summary={headerSummary}>
      <form className="search-row" onSubmit={submitSearch}>
        <input
          className="search-input"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          placeholder="ابحث بالاسم أو الكود..."
          aria-label="بحث عن عميل"
        />
        <button className="primary-button" type="submit">بحث</button>
      </form>

      {customersQuery.isLoading ? <LoadingState /> : null}

      {customersQuery.isError ? (
        <ErrorState
          message={getErrorMessage(customersQuery.error)}
          onRetry={() => void customersQuery.refetch()}
        />
      ) : null}

      {customersQuery.isSuccess && customersQuery.data.items.length === 0 ? (
        <EmptyState title="لا يوجد عملاء" description="لم يتم العثور على عملاء مطابقين لبحثك." />
      ) : null}

      {customersQuery.isSuccess && customersQuery.data.items.length > 0 ? (
        <>
          <section className="card-list" aria-label="قائمة العملاء">
            {customersQuery.data.items.map((customer) => (
              <Link className="card-link" key={customer.id} to={`/customers/${customer.id}`}>
                <CustomerListCard customer={customer} />
              </Link>
            ))}
          </section>
          <Pagination
            page={customersQuery.data.page}
            totalPages={customersQuery.data.totalPages}
            totalCount={customersQuery.data.totalCount}
            onPrevious={() => setPage((current) => Math.max(1, current - 1))}
            onNext={() => setPage((current) => current + 1)}
          />
        </>
      ) : null}
    </AppShell>
  );
}

function CustomerListCard({ customer }: { customer: CustomerListDto }) {
  const subtitle = `${customer.code} • ${customerTypeLabels[customer.type as CustomerType]}`;

  return (
    <DataCard
      icon={<Icon name="customers" />}
      title={customer.nameAr}
      subtitle={subtitle}
      meta={formatCurrency(customer.balance)}
      value={<CustomerStatusPill status={customer.status} />}
      tone={customer.status === 0 ? 'available' : 'low'}
    />
  );
}

function CustomerStatusPill({ status }: { status: CustomerStatus }) {
  return (
    <span className={`status-pill status-pill--${getCustomerStatusTone(status)}`}>
      {customerStatusLabels[status]}
    </span>
  );
}

type DetailsTab = 'sales' | 'statement';

function CustomerDetailsPage({ customerId }: { customerId: string }) {
  const [activeTab, setActiveTab] = useState<DetailsTab>('statement');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');

  const detailsQuery = useQuery({
    queryKey: ['customer-details', customerId],
    queryFn: () => getCustomerDetails(customerId)
  });

  const customer = detailsQuery.data;

  const headerSummary = customer ? (
    <>
      <SummaryCard label="الرصيد" value={formatCurrency(customer.balance)} tone={customer.balance > 0 ? 'amber' : 'green'} />
      <SummaryCard label="حد الائتمان" value={formatCurrency(customer.creditLimit)} />
    </>
  ) : undefined;

  return (
    <AppShell title={customer ? customer.nameAr : 'تفاصيل العميل'} summary={headerSummary}>
      {detailsQuery.isLoading ? <LoadingState /> : null}

      {detailsQuery.isError ? (
        <ErrorState message={getErrorMessage(detailsQuery.error)} onRetry={() => void detailsQuery.refetch()} />
      ) : null}

      {customer ? (
        <div className="details-stack">
          <section className="detail-card">
            <h2>بيانات العميل</h2>
            <dl className="detail-grid">
              <DetailItem label="الكود" value={customer.code} />
              <DetailItem label="النوع" value={customerTypeLabels[customer.type as CustomerType]} />
              <DetailItem label="الحالة" value={customerStatusLabels[customer.status as CustomerStatus]} />
              <DetailItem label="شروط الدفع" value={`${formatNumber(customer.paymentTermsDays)} يوم`} />
              <DetailItem label="الهاتف" value={customer.phone ?? 'غير محدد'} />
              <DetailItem label="البريد" value={customer.email ?? 'غير محدد'} />
            </dl>
          </section>

          <section className="detail-card">
            <div className="section-title-row tab-row">
              <button
                className={`filter-chip ${activeTab === 'statement' ? 'filter-chip--active' : ''}`}
                type="button"
                onClick={() => setActiveTab('statement')}
              >
                كشف الحساب
              </button>
              <button
                className={`filter-chip ${activeTab === 'sales' ? 'filter-chip--active' : ''}`}
                type="button"
                onClick={() => setActiveTab('sales')}
              >
                تفاصيل المبيعات
              </button>
            </div>

            <div className="form-grid">
              <label>
                من تاريخ
                <input type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
              </label>
              <label>
                إلى تاريخ
                <input type="date" value={to} onChange={(event) => setTo(event.target.value)} />
              </label>
            </div>

            {activeTab === 'statement' ? (
              <CustomerStatementPanel customerId={customerId} from={from} to={to} />
            ) : (
              <CustomerSalesDetailsPanel customerId={customerId} from={from} to={to} />
            )}
          </section>
        </div>
      ) : null}
    </AppShell>
  );
}

function CustomerStatementPanel({ customerId, from, to }: { customerId: string; from: string; to: string }) {
  const statementQuery = useQuery({
    queryKey: ['customer-statement', customerId, from, to],
    queryFn: () => getCustomerStatement(customerId, { from: from || undefined, to: to || undefined })
  });

  if (statementQuery.isLoading) {
    return <LoadingState />;
  }

  if (statementQuery.isError) {
    return <ErrorState message={getErrorMessage(statementQuery.error)} onRetry={() => void statementQuery.refetch()} />;
  }

  if (!statementQuery.data) {
    return null;
  }

  const statement = statementQuery.data;

  return (
    <>
      <dl className="mini-grid">
        <DetailItem label="الرصيد الافتتاحي" value={formatCurrency(statement.openingBalance)} />
        <DetailItem label="الرصيد الختامي" value={formatCurrency(statement.closingBalance)} />
      </dl>

      {statement.lines.length === 0 ? (
        <EmptyState title="لا توجد حركات" description="لا توجد حركات ضمن الفترة المحددة." />
      ) : (
        <div className="table-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th>التاريخ</th>
                <th>نوع المستند</th>
                <th>رقم المستند</th>
                <th>مدين</th>
                <th>دائن</th>
                <th>الرصيد</th>
              </tr>
            </thead>
            <tbody>
              {statement.lines.map((line, index) => (
                <tr key={`${line.documentNumber}-${index}`}>
                  <td>{formatDateOnly(line.entryDate)}</td>
                  <td>{documentTypeLabels[line.documentType] ?? line.documentType}</td>
                  <td>{line.documentNumber}</td>
                  <td>{line.debit > 0 ? formatCurrency(line.debit) : '—'}</td>
                  <td>{line.credit > 0 ? formatCurrency(line.credit) : '—'}</td>
                  <td>{formatCurrency(line.runningBalance)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}

function CustomerSalesDetailsPanel({ customerId, from, to }: { customerId: string; from: string; to: string }) {
  const salesQuery = useQuery({
    queryKey: ['customer-sales-details', customerId, from, to],
    queryFn: () => getCustomerSalesDetails(customerId, { from: from || undefined, to: to || undefined })
  });

  if (salesQuery.isLoading) {
    return <LoadingState />;
  }

  if (salesQuery.isError) {
    return <ErrorState message={getErrorMessage(salesQuery.error)} onRetry={() => void salesQuery.refetch()} />;
  }

  if (!salesQuery.data) {
    return null;
  }

  if (salesQuery.data.length === 0) {
    return <EmptyState title="لا توجد مبيعات" description="لا توجد مبيعات ضمن الفترة المحددة." />;
  }

  return (
    <div className="table-scroll">
      <table className="data-table">
        <thead>
          <tr>
            <th>التاريخ</th>
            <th>الصنف</th>
            <th>الكود</th>
            <th>اللون</th>
            <th>سعر الوحدة</th>
          </tr>
        </thead>
        <tbody>
          {salesQuery.data.map((line, index) => (
            <tr key={index}>
              <td>{formatDateOnly(line.saleDate)}</td>
              <td>{line.fabricName}</td>
              <td>{line.fabricCode}</td>
              <td>{line.colorName}</td>
              <td>{formatCurrency(line.unitPrice)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function DetailItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}

function Pagination({
  page,
  totalPages,
  totalCount,
  onPrevious,
  onNext
}: {
  page: number;
  totalPages: number;
  totalCount: number;
  onPrevious: () => void;
  onNext: () => void;
}) {
  const hasPreviousPage = page > 1;
  const hasNextPage = page < totalPages;

  return (
    <nav className="pagination" aria-label="تنقل الصفحات">
      <button className="primary-button" type="button" disabled={!hasPreviousPage} onClick={onPrevious}>السابق</button>
      <span>صفحة {formatNumber(page)} من {formatNumber(Math.max(totalPages, 1))} • {formatNumber(totalCount)} عميل</span>
      <button className="primary-button" type="button" disabled={!hasNextPage} onClick={onNext}>التالي</button>
    </nav>
  );
}

function getErrorMessage(error: unknown) {
  if (error instanceof ApiError) {
    if (error.status === 403) {
      return 'لا تملك صلاحية لهذا الإجراء.';
    }
    if (error.status === 404) {
      return 'العميل غير موجود.';
    }
    return error.message;
  }
  return 'حدث خطأ غير متوقع.';
}
