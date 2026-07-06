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
import { RecordField } from '../components/RecordField.tsx';
import { SummaryCard } from '../components/SummaryCard.tsx';
import { formatCurrency, formatDateOnly, formatNumber } from '../lib/format.ts';
import { customerStatusLabels, customerTypeLabels, documentTypeLabels, getCustomerStatusTone } from '../lib/enums.ts';

const LIST_PAGE_SIZE = 500;

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

  const customersQuery = useQuery({
    queryKey: ['customers', search],
    queryFn: () => getCustomers({ search: search || undefined, page: 1, pageSize: LIST_PAGE_SIZE })
  });

  const listSummary = useMemo(() => {
    const rows = customersQuery.data?.items ?? [];
    return {
      count: customersQuery.data?.totalCount ?? rows.length,
      outstanding: rows.reduce((sum, row) => sum + row.balance, 0)
    };
  }, [customersQuery.data?.items, customersQuery.data?.totalCount]);

  const headerSummary = (
    <>
      <SummaryCard label="عدد العملاء" value={formatNumber(listSummary.count)} />
      <SummaryCard label="إجمالي الأرصدة" value={formatCurrency(listSummary.outstanding)} tone="amber" />
    </>
  );

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSearch(searchInput.trim());
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
        <section className="card-list" aria-label="قائمة العملاء">
          {customersQuery.data.items.map((customer) => (
            <Link className="card-link" key={customer.id} to={`/customers/${customer.id}`}>
              <CustomerListCard customer={customer} />
            </Link>
          ))}
        </section>
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
    <AppShell title="تفاصيل العميل" summary={headerSummary}>
      {detailsQuery.isLoading ? <LoadingState /> : null}

      {detailsQuery.isError ? (
        <ErrorState message={getErrorMessage(detailsQuery.error)} onRetry={() => void detailsQuery.refetch()} />
      ) : null}

      {customer ? (
        <div className="details-stack">
          <section className="detail-card detail-card--hero">
            <div className="detail-card__lead">
              <p className="detail-card__eyebrow">{customer.code}</p>
              <h2>{customer.nameAr}</h2>
              <CustomerStatusPill status={customer.status} />
            </div>
          </section>

          <section className="detail-card">
            <h2>بيانات العميل</h2>
            <dl className="detail-grid">
              <DetailItem label="النوع" value={customerTypeLabels[customer.type as CustomerType]} />
              <DetailItem label="شروط الدفع" value={`${formatNumber(customer.paymentTermsDays)} يوم`} />
              <DetailItem label="الهاتف" value={customer.phone ?? 'غير محدد'} />
              <DetailItem label="البريد" value={customer.email ?? 'غير محدد'} />
            </dl>
          </section>

          <section className="detail-card detail-card--tabs">
            <div className="tab-strip" role="tablist" aria-label="تبويبات العميل">
              <button
                className={`filter-chip ${activeTab === 'statement' ? 'filter-chip--active' : ''}`}
                type="button"
                role="tab"
                aria-selected={activeTab === 'statement'}
                onClick={() => setActiveTab('statement')}
              >
                كشف الحساب
              </button>
              <button
                className={`filter-chip ${activeTab === 'sales' ? 'filter-chip--active' : ''}`}
                type="button"
                role="tab"
                aria-selected={activeTab === 'sales'}
                onClick={() => setActiveTab('sales')}
              >
                تفاصيل المبيعات
              </button>
            </div>

            <div className="form-grid form-grid--dates">
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
        <>
          <div className="record-list mobile-only" aria-label="حركات كشف الحساب">
            {statement.lines.map((line, index) => (
              <article className="record-card" key={`${line.documentNumber}-${index}`}>
                <div className="record-card__head">
                  <strong className="record-card__title">{formatDateOnly(line.entryDate)}</strong>
                  <span className="record-card__badge">{line.documentNumber}</span>
                </div>
                <p className="record-card__meta">{documentTypeLabels[line.documentType] ?? line.documentType}</p>
                <dl className="record-card__grid">
                  <RecordField label="مدين" value={line.debit > 0 ? formatCurrency(line.debit) : '—'} />
                  <RecordField label="دائن" value={line.credit > 0 ? formatCurrency(line.credit) : '—'} />
                  <RecordField label="الرصيد" value={formatCurrency(line.runningBalance)} emphasis />
                </dl>
              </article>
            ))}
          </div>
          <div className="table-scroll desktop-only">
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
        </>
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
    <>
      <div className="record-list mobile-only" aria-label="تفاصيل المبيعات">
        {salesQuery.data.map((line, index) => (
          <article className="record-card" key={index}>
            <div className="record-card__head">
              <strong className="record-card__title">{line.fabricName}</strong>
              <span className="record-card__badge">{formatDateOnly(line.saleDate)}</span>
            </div>
            <p className="record-card__meta">
              {line.fabricCode} • {line.colorName}
            </p>
            <dl className="record-card__grid record-card__grid--single">
              <RecordField label="سعر الوحدة" value={formatCurrency(line.unitPrice)} emphasis />
            </dl>
          </article>
        ))}
      </div>
      <div className="table-scroll desktop-only">
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
    </>
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
