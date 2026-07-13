using System.Diagnostics;
using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Mapping;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Finance;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Application.Common;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

internal sealed class OpeningBalanceEngine(
    ErpDbContext context,
    IOpeningBalanceRepository repository,
    IOpeningBalanceLookupService lookups,
    IIntegratedAccountingService accounting,
    IInventoryEngine inventoryEngine,
    INumberingService numbering,
    IUnitOfWork unitOfWork,
    ICurrentUserService user,
    ICurrentBranchService branch,
    ICustomerRepository customerRepository,
    ISupplierRepository supplierRepository,
    IFabricCatalogRepository fabricCatalogRepository) : IOpeningBalanceEngine
{
    public byte[] BuildImportTemplate(OpeningBalanceType type) =>
        OpeningBalanceExcelParser.BuildTemplate(type);

    public async Task<OpeningBalanceValidationReportDto> ValidateAsync(
        ValidateOpeningBalanceCommand command,
        CancellationToken cancellationToken = default)
    {
        var companyId = branch.CompanyId ?? Guid.Empty;
        var lookup = await lookups.GetLookupsAsync(companyId, cancellationToken);
        var existingKeys = await repository.GetExistingLineKeysAsync(
            companyId, command.Type, command.ExcludeDocumentId, cancellationToken);

        var errors = new List<OpeningBalanceValidationIssueDto>();
        var warnings = new List<OpeningBalanceValidationIssueDto>();
        var validRows = 0;
        var duplicateRows = 0;

        if (command.Lines.Count == 0)
        {
            errors.Add(new() { RowNumber = 0, Field = "Lines", Message = "يجب إدخال سطر واحد على الأقل." });
            return BuildReport(command.Lines.Count, 0, duplicateRows, errors, warnings);
        }

        if (command.ExchangeRate <= 0)
            errors.Add(new() { RowNumber = 0, Field = "ExchangeRate", Message = "سعر الصرف يجب أن يكون أكبر من صفر." });

        if (string.IsNullOrWhiteSpace(command.CurrencyCode) || command.CurrencyCode.Trim().Length != 3)
            errors.Add(new() { RowNumber = 0, Field = "Currency", Message = "رمز العملة غير صالح (3 أحرف)." });

        var row = 0;
        foreach (var input in command.Lines)
        {
            row++;
            var line = OpeningBalanceMappers.ToDomainLine(input);
            ValidateLine(command.Type, row, input, line, lookup, errors, warnings);

            var key = OpeningBalanceMapper.BuildLineKey(command.Type, line);
            if (!string.IsNullOrWhiteSpace(key) && existingKeys.Contains(key))
            {
                duplicateRows++;
                errors.Add(new() { RowNumber = row, Field = "Duplicate", Message = $"سطر مكرر: {key}" });
                continue;
            }

            if (input.PartyId is Guid pid && command.Type == OpeningBalanceType.CustomerReceivable)
            {
                var posted = await context.Customers.AsNoTracking()
                    .AnyAsync(c => c.Id == pid && c.OpeningBalancePosted, cancellationToken);
                if (posted)
                    errors.Add(new() { RowNumber = row, Field = "Customer", Message = "العميل لديه رصيد افتتاحي مرحّل مسبقاً." });
            }

            if (input.PartyId is Guid spid && command.Type == OpeningBalanceType.SupplierPayable)
            {
                var posted = await context.Suppliers.AsNoTracking()
                    .AnyAsync(s => s.Id == spid && s.OpeningBalancePosted, cancellationToken);
                if (posted)
                    errors.Add(new() { RowNumber = row, Field = "Supplier", Message = "المورد لديه رصيد افتتاحي مرحّل مسبقاً." });
            }

            if (!errors.Any(e => e.RowNumber == row))
                validRows++;
        }

        if (command.Type == OpeningBalanceType.GeneralLedger)
        {
            var totalDebit = command.Lines.Sum(l => l.Debit);
            var totalCredit = command.Lines.Sum(l => l.Credit);
            if (Math.Abs(totalDebit - totalCredit) > 0.01m)
                errors.Add(new() { RowNumber = 0, Field = "Balance", Message = "مجموع المدين يجب أن يساوي مجموع الدائن في أرصدة الدفتر." });
        }

        return BuildReport(command.Lines.Count, validRows, duplicateRows, errors, warnings);
    }

    public async Task<ApplicationResult<OpeningBalanceListDto>> CreateAsync(
        CreateOpeningBalanceCommand command,
        CancellationToken cancellationToken = default)
    {
        var companyId = branch.CompanyId ?? Guid.Empty;
        var branchId = branch.BranchId ?? Guid.Empty;
        if (companyId == Guid.Empty || branchId == Guid.Empty)
            return ApplicationResult<OpeningBalanceListDto>.Failure("سياق الشركة/الفرع غير محدد.");

        var validation = await ValidateAsync(new ValidateOpeningBalanceCommand
        {
            Type = command.Type,
            CurrencyCode = command.CurrencyCode,
            ExchangeRate = command.ExchangeRate,
            OpeningDate = command.OpeningDate,
            Lines = command.Lines
        }, cancellationToken);

        if (!validation.IsValid)
            return ApplicationResult<OpeningBalanceListDto>.ValidationFailed(
                validation.Errors.Select(e => new ValidationError(e.Field, e.Message)));

        var resolved = await ResolveLinesAsync(command.Type, command.Lines, cancellationToken);
        var number = await numbering.NextOpeningBalanceNumberAsync(branchId, cancellationToken);
        var doc = OpeningBalanceDocument.Create(
            companyId, branchId, number, command.Type, command.Source,
            command.OpeningDate, command.CurrencyCode, command.ExchangeRate,
            command.Reference, command.Description, command.Notes, user.UserId);
        doc.ReplaceLines(resolved);

        if (command.SubmitForApproval)
            doc.SubmitForApproval();

        await repository.AddAsync(doc, cancellationToken);
        await OpeningBalanceTrailRecorder.RecordAsync(
            repository, user, doc.Id, command.Source == OpeningBalanceSource.ExcelImport ? "Imported" : "Created",
            notes: $"{OpeningBalanceDisplay.TypeName(command.Type)} — {doc.Number}", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApplicationResult<OpeningBalanceListDto>.Success(OpeningBalanceMappers.ToListDto(doc));
    }

    public async Task<ApplicationResult<OpeningBalanceListDto>> UpdateAsync(
        UpdateOpeningBalanceCommand command,
        CancellationToken cancellationToken = default)
    {
        var doc = await repository.GetAsync(command.DocumentId, cancellationToken);
        if (doc is null)
            return ApplicationResult<OpeningBalanceListDto>.NotFound("المستند غير موجود.");

        var validation = await ValidateAsync(new ValidateOpeningBalanceCommand
        {
            Type = doc.Type,
            CurrencyCode = command.CurrencyCode,
            ExchangeRate = command.ExchangeRate,
            OpeningDate = command.OpeningDate,
            Lines = command.Lines,
            ExcludeDocumentId = doc.Id
        }, cancellationToken);

        if (!validation.IsValid)
            return ApplicationResult<OpeningBalanceListDto>.ValidationFailed(
                validation.Errors.Select(e => new ValidationError(e.Field, e.Message)));

        doc.UpdateHeader(command.OpeningDate, command.CurrencyCode, command.ExchangeRate,
            command.Reference, command.Description, command.Notes);
        doc.ReplaceLines(await ResolveLinesAsync(doc.Type, command.Lines, cancellationToken));

        await repository.UpdateAsync(doc, cancellationToken);
        await OpeningBalanceTrailRecorder.RecordAsync(repository, user, doc.Id, "Edited", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OpeningBalanceListDto>.Success(OpeningBalanceMappers.ToListDto(doc));
    }

    public async Task<ApplicationResult> SubmitAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var doc = await repository.GetAsync(documentId, cancellationToken);
        if (doc is null) return ApplicationResult.NotFound("المستند غير موجود.");
        if (doc.Status is not (OpeningBalanceStatus.Draft or OpeningBalanceStatus.Rejected))
            return ApplicationResult.Success();
        doc.SubmitForApproval();
        await repository.UpdateAsync(doc, cancellationToken);
        await OpeningBalanceTrailRecorder.RecordAsync(repository, user, doc.Id, "Submitted", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> ApproveAsync(Guid documentId, string? notes, CancellationToken cancellationToken = default)
    {
        var doc = await repository.GetAsync(documentId, cancellationToken);
        if (doc is null) return ApplicationResult.NotFound("المستند غير موجود.");
        if (doc.Status is OpeningBalanceStatus.Approved or OpeningBalanceStatus.Posted or OpeningBalanceStatus.Locked)
            return ApplicationResult.Success();
        if (doc.Status is OpeningBalanceStatus.PendingApproval or OpeningBalanceStatus.Draft)
        {
            doc.Approve(user.UserId, notes);
            await repository.UpdateAsync(doc, cancellationToken);
            await OpeningBalanceTrailRecorder.RecordAsync(repository, user, doc.Id, "Approved", notes: notes, cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> RejectAsync(Guid documentId, string reason, CancellationToken cancellationToken = default)
    {
        var doc = await repository.GetAsync(documentId, cancellationToken);
        if (doc is null) return ApplicationResult.NotFound("المستند غير موجود.");
        doc.Reject(reason);
        await repository.UpdateAsync(doc, cancellationToken);
        await OpeningBalanceTrailRecorder.RecordAsync(repository, user, doc.Id, "Rejected", notes: reason, cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<OpeningBalancePostResultDto>> PostAsync(
        Guid documentId,
        bool lockAfterPost,
        CancellationToken cancellationToken = default)
    {
        var doc = await repository.GetAsync(documentId, cancellationToken);
        if (doc is null)
            return ApplicationResult<OpeningBalancePostResultDto>.NotFound("المستند غير موجود.");

        if (doc.Status is OpeningBalanceStatus.Posted or OpeningBalanceStatus.Locked)
        {
            return ApplicationResult<OpeningBalancePostResultDto>.Success(new OpeningBalancePostResultDto
            {
                DocumentId = doc.Id,
                DocumentNumber = doc.Number,
                JournalEntryNumber = doc.JournalEntryNumber ?? doc.Number,
                PostedAt = doc.PostedAt ?? DateTime.UtcNow,
                TotalBaseAmount = doc.TotalBaseAmount
            });
        }

        if (doc.Status != OpeningBalanceStatus.Approved)
            return ApplicationResult<OpeningBalancePostResultDto>.ValidationFailed("Status", "يجب اعتماد المستند قبل الترحيل.");

        var journalLines = await BuildJournalLinesAsync(doc, cancellationToken);
        var description = $"رصيد افتتاحي {OpeningBalanceDisplay.TypeName(doc.Type)} — {doc.Number}";
        var journalNumber = await accounting.PostOpeningBalanceDocumentAsync(
            doc.Id, doc.Number, description, doc.OpeningDate, journalLines, cancellationToken);

        if (doc.Type == OpeningBalanceType.OpeningStock)
        {
            var movementIds = await inventoryEngine.PostFinanceOpeningBalanceStockAsync(doc.Id, cancellationToken);
            await OpeningBalanceTrailRecorder.RecordAsync(
                repository, user, doc.Id, "InventoryPosted",
                newValues: string.Join(",", movementIds),
                cancellationToken: cancellationToken);
        }

        await ApplyPartyEffectsAsync(doc, cancellationToken);

        doc.MarkPosted(user.UserId, journalNumber);
        if (lockAfterPost)
            doc.Lock();

        await repository.UpdateAsync(doc, cancellationToken);
        await OpeningBalanceTrailRecorder.RecordAsync(
            repository, user, doc.Id, lockAfterPost ? "PostedAndLocked" : "Posted",
            newValues: journalNumber, cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApplicationResult<OpeningBalancePostResultDto>.Success(new OpeningBalancePostResultDto
        {
            DocumentId = doc.Id,
            DocumentNumber = doc.Number,
            JournalEntryNumber = journalNumber,
            PostedAt = doc.PostedAt ?? DateTime.UtcNow,
            TotalBaseAmount = doc.TotalBaseAmount
        });
    }

    public async Task<ApplicationResult<OpeningBalancePostResultDto>> PostPartyOpeningBalanceAsync(
        PostPartyOpeningBalanceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.PartyId == Guid.Empty)
            return ApplicationResult<OpeningBalancePostResultDto>.ValidationFailed(nameof(command.PartyId), "الطرف مطلوب.");
        if (command.Amount <= 0)
            return ApplicationResult<OpeningBalancePostResultDto>.ValidationFailed(nameof(command.Amount), "المبلغ يجب أن يكون أكبر من صفر.");
        if (command.Type is not (OpeningBalanceType.CustomerReceivable or OpeningBalanceType.SupplierPayable))
            return ApplicationResult<OpeningBalancePostResultDto>.ValidationFailed(nameof(command.Type), "نوع غير مدعوم لترحيل الأطراف.");

        var isCustomer = command.Type == OpeningBalanceType.CustomerReceivable;
        var partyName = command.PartyName;
        if (string.IsNullOrWhiteSpace(partyName))
        {
            partyName = isCustomer
                ? await context.Customers.AsNoTracking()
                    .Where(c => c.Id == command.PartyId)
                    .Select(c => c.NameAr)
                    .FirstOrDefaultAsync(cancellationToken)
                : await context.Suppliers.AsNoTracking()
                    .Where(s => s.Id == command.PartyId)
                    .Select(s => s.NameAr)
                    .FirstOrDefaultAsync(cancellationToken);
        }

        if (isCustomer)
        {
            var posted = await context.Customers.AsNoTracking()
                .AnyAsync(c => c.Id == command.PartyId && c.OpeningBalancePosted, cancellationToken);
            if (posted)
                return ApplicationResult<OpeningBalancePostResultDto>.ValidationFailed(nameof(command.PartyId), "العميل لديه رصيد افتتاحي مرحّل مسبقاً.");
        }
        else
        {
            var posted = await context.Suppliers.AsNoTracking()
                .AnyAsync(s => s.Id == command.PartyId && s.OpeningBalancePosted, cancellationToken);
            if (posted)
                return ApplicationResult<OpeningBalancePostResultDto>.ValidationFailed(nameof(command.PartyId), "المورد لديه رصيد افتتاحي مرحّل مسبقاً.");
        }

        var line = new OpeningBalanceLineInput
        {
            PartyId = command.PartyId,
            PartyName = partyName,
            Debit = isCustomer ? command.Amount : 0,
            Credit = isCustomer ? 0 : command.Amount,
            Description = command.ReferenceNote
        };

        var create = await CreateAsync(new CreateOpeningBalanceCommand
        {
            Type = command.Type,
            Source = OpeningBalanceSource.Manual,
            OpeningDate = command.OpeningDate,
            Reference = command.ReferenceNote,
            Description = command.ReferenceNote ?? $"رصيد افتتاحي — {command.PartyName}",
            Lines = [line]
        }, cancellationToken);

        if (!create.IsSuccess || create.Value is null)
            return ApplicationResult<OpeningBalancePostResultDto>.Failure(create.ErrorMessage ?? "فشل إنشاء المستند.");

        var docId = create.Value.Id;
        var submit = await SubmitAsync(docId, cancellationToken);
        if (!submit.IsSuccess)
            return ApplicationResult<OpeningBalancePostResultDto>.Failure(submit.ErrorMessage ?? "فشل الإرسال للاعتماد.");

        var approve = await ApproveAsync(docId, "ترحيل من وحدة الأطراف", cancellationToken);
        if (!approve.IsSuccess)
            return ApplicationResult<OpeningBalancePostResultDto>.Failure(approve.ErrorMessage ?? "فشل الاعتماد.");

        return await PostAsync(docId, lockAfterPost: true, cancellationToken);
    }

    public async Task<ApplicationResult> ArchiveAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var doc = await repository.GetAsync(documentId, cancellationToken);
        if (doc is null) return ApplicationResult.NotFound("المستند غير موجود.");
        doc.Archive();
        await repository.UpdateAsync(doc, cancellationToken);
        await OpeningBalanceTrailRecorder.RecordAsync(repository, user, doc.Id, "Archived", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<OpeningBalanceListDto>> DuplicateAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var source = await repository.GetAsync(documentId, cancellationToken);
        if (source is null)
            return ApplicationResult<OpeningBalanceListDto>.NotFound("المستند غير موجود.");

        var lines = source.Lines.Select(l => new OpeningBalanceLineInput
        {
            PartyId = l.PartyId, PartyName = l.PartyName,
            AccountId = l.AccountId, AccountName = l.AccountName,
            WarehouseId = l.WarehouseId, WarehouseName = l.WarehouseName,
            FabricItemId = l.FabricItemId, FabricColorId = l.FabricColorId,
            ItemName = l.ItemName, ColorName = l.ColorName, BatchNumber = l.BatchNumber,
            LocationCode = l.LocationCode, RollCount = l.RollCount, Quantity = l.Quantity,
            UnitCost = l.UnitCost, BankName = l.BankName, BankAccountNumber = l.BankAccountNumber,
            InvestmentScope = l.InvestmentScope, Debit = l.Debit, Credit = l.Credit,
            Reference = l.Reference, Description = l.Description, Notes = l.Notes
        }).ToList();

        return await CreateAsync(new CreateOpeningBalanceCommand
        {
            Type = source.Type,
            Source = OpeningBalanceSource.Manual,
            OpeningDate = source.OpeningDate,
            CurrencyCode = source.CurrencyCode,
            ExchangeRate = source.ExchangeRate,
            Reference = source.Reference,
            Description = $"نسخة من {source.Number}",
            Notes = source.Notes,
            Lines = lines
        }, cancellationToken);
    }

    public async Task<ApplicationResult<OpeningBalanceImportResultDto>> ImportExcelAsync(
        ImportOpeningBalanceExcelCommand command,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var (_, lines, templateErrors) = OpeningBalanceExcelParser.Parse(command.Type, command.Content);
        if (templateErrors.Count > 0)
        {
            return ApplicationResult<OpeningBalanceImportResultDto>.ValidationFailed(
                templateErrors.Select((e, i) => new ValidationError($"Template[{i}]", e)));
        }

        var validation = await ValidateAsync(new ValidateOpeningBalanceCommand
        {
            Type = command.Type,
            CurrencyCode = command.CurrencyCode,
            ExchangeRate = command.ExchangeRate,
            OpeningDate = command.OpeningDate,
            Lines = lines
        }, cancellationToken);

        if (command.PreviewOnly)
        {
            return ApplicationResult<OpeningBalanceImportResultDto>.Success(new OpeningBalanceImportResultDto
            {
                TotalRows = lines.Count,
                ImportedRows = validation.ValidRows,
                SkippedRows = lines.Count - validation.ValidRows,
                DuplicateRows = validation.DuplicateRows,
                WarningCount = validation.Warnings.Count,
                ErrorCount = validation.Errors.Count,
                ExecutionMs = sw.ElapsedMilliseconds,
                UserName = user.Username ?? "system",
                ImportDate = DateTime.UtcNow,
                FileName = command.FileName,
                Validation = validation
            });
        }

        if (!validation.IsValid && !command.SkipInvalidRows)
            return ApplicationResult<OpeningBalanceImportResultDto>.ValidationFailed(
                validation.Errors.Select(e => new ValidationError(e.Field, e.Message)));

        var importLines = command.SkipInvalidRows
            ? FilterValidLines(lines, validation)
            : lines;

        var create = await CreateAsync(new CreateOpeningBalanceCommand
        {
            Type = command.Type,
            Source = OpeningBalanceSource.ExcelImport,
            OpeningDate = command.OpeningDate,
            CurrencyCode = command.CurrencyCode,
            ExchangeRate = command.ExchangeRate,
            Reference = command.Reference ?? command.FileName,
            Description = $"استيراد Excel — {command.FileName}",
            Lines = importLines,
            SubmitForApproval = false
        }, cancellationToken);

        if (!create.IsSuccess || create.Value is null)
            return ApplicationResult<OpeningBalanceImportResultDto>.Failure(create.ErrorMessage ?? "فشل الاستيراد.");

        sw.Stop();
        return ApplicationResult<OpeningBalanceImportResultDto>.Success(new OpeningBalanceImportResultDto
        {
            DocumentId = create.Value.Id,
            DocumentNumber = create.Value.Number,
            TotalRows = lines.Count,
            ImportedRows = importLines.Count,
            SkippedRows = lines.Count - importLines.Count,
            DuplicateRows = validation.DuplicateRows,
            WarningCount = validation.Warnings.Count,
            ErrorCount = validation.Errors.Count,
            ExecutionMs = sw.ElapsedMilliseconds,
            UserName = user.Username ?? "system",
            ImportDate = DateTime.UtcNow,
            FileName = command.FileName,
            Validation = validation
        });
    }

    private static List<OpeningBalanceLineInput> FilterValidLines(
        IReadOnlyList<OpeningBalanceLineInput> lines,
        OpeningBalanceValidationReportDto validation)
    {
        var badRows = validation.Errors.Select(e => e.RowNumber).Where(r => r > 0).ToHashSet();
        var result = new List<OpeningBalanceLineInput>();
        for (var i = 0; i < lines.Count; i++)
        {
            if (!badRows.Contains(i + 1))
                result.Add(lines[i]);
        }

        return result;
    }

    private async Task<List<OpeningBalanceLine>> ResolveLinesAsync(
        OpeningBalanceType type,
        IReadOnlyList<OpeningBalanceLineInput> inputs,
        CancellationToken cancellationToken)
    {
        var companyId = branch.CompanyId ?? Guid.Empty;
        var lookup = await lookups.GetLookupsAsync(companyId, cancellationToken);
        var resolved = new List<OpeningBalanceLine>();
        var openingStockCatalog = type == OpeningBalanceType.OpeningStock
            ? new OpeningBalanceFabricCatalogBatch()
            : null;

        foreach (var input in inputs)
        {
            var partyId = input.PartyId;
            var partyName = input.PartyName;
            var accountId = input.AccountId;
            var accountName = input.AccountName;
            var warehouseId = input.WarehouseId;
            var warehouseName = input.WarehouseName;
            var fabricItemId = input.FabricItemId;
            var fabricColorId = input.FabricColorId;
            var itemName = input.ItemName?.Trim();
            var colorName = input.ColorName?.Trim();

            if (openingStockCatalog is not null &&
                (fabricItemId is null || fabricItemId == Guid.Empty ||
                 fabricColorId is null || fabricColorId == Guid.Empty))
            {
                var catalogEntry = await openingStockCatalog.EnsureAsync(
                    fabricCatalogRepository,
                    companyId,
                    itemName ?? "",
                    colorName ?? "",
                    cancellationToken);
                fabricItemId = catalogEntry.Item.Id;
                fabricColorId = catalogEntry.Color.Id;
                itemName = catalogEntry.Item.NameAr;
                colorName = catalogEntry.Color.NameAr;
            }

            if (partyId is null && !string.IsNullOrWhiteSpace(partyName))
            {
                var pool = type switch
                {
                    OpeningBalanceType.CustomerReceivable => lookup.Customers,
                    OpeningBalanceType.SupplierPayable => lookup.Suppliers,
                    OpeningBalanceType.Capital => lookup.Partners,
                    _ => lookup.Customers
                };
                var match = pool.FirstOrDefault(p =>
                    p.Name.Equals(partyName, StringComparison.OrdinalIgnoreCase) ||
                    p.Code.Equals(partyName, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                {
                    partyId = match.Id;
                    partyName = match.Name;
                }
            }

            if (warehouseId is null && !string.IsNullOrWhiteSpace(warehouseName))
            {
                var w = lookup.Warehouses.FirstOrDefault(x =>
                    x.Name.Equals(warehouseName, StringComparison.OrdinalIgnoreCase) ||
                    x.Code.Equals(warehouseName, StringComparison.OrdinalIgnoreCase));
                if (w is not null)
                {
                    warehouseId = w.Id;
                    warehouseName = w.Name;
                }
            }

            if (accountId is null && !string.IsNullOrWhiteSpace(accountName))
            {
                if (type == OpeningBalanceType.Cash)
                {
                    var cb = lookup.Cashboxes.FirstOrDefault(c =>
                        c.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase) ||
                        c.Code.Equals(accountName, StringComparison.OrdinalIgnoreCase));
                    if (cb is not null)
                    {
                        accountId = cb.Id;
                        accountName = cb.Name;
                    }
                }
                else
                {
                    var acc = lookup.Accounts.FirstOrDefault(a =>
                        a.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase) ||
                        a.Code.Equals(accountName, StringComparison.OrdinalIgnoreCase));
                    if (acc is not null)
                    {
                        accountId = acc.Id;
                        accountName = acc.Name;
                    }
                }
            }

            var debit = input.Debit;
            var credit = input.Credit;
            if (type == OpeningBalanceType.OpeningStock && debit == 0 && credit == 0)
            {
                var qty = input.Quantity ?? 0;
                var cost = input.UnitCost ?? 0;
                debit = qty * cost;
            }

            if (type is OpeningBalanceType.CustomerReceivable or OpeningBalanceType.SupplierPayable
                or OpeningBalanceType.Cash or OpeningBalanceType.Bank && debit == 0 && credit == 0)
            {
                var amt = Math.Max(debit, credit);
                if (type == OpeningBalanceType.SupplierPayable || type == OpeningBalanceType.Capital)
                    credit = amt;
                else
                    debit = amt;
            }

            resolved.Add(OpeningBalanceLine.Create(
                debit, credit, partyId, partyName, accountId, accountName,
                warehouseId, warehouseName, fabricItemId, fabricColorId,
                itemName, colorName, input.BatchNumber,
                input.LocationCode, input.RollCount, input.Quantity, input.UnitCost,
                input.BankName, input.BankAccountNumber, input.InvestmentScope,
                input.Reference, input.Description, input.Notes));
        }

        return resolved;
    }

    private async Task<List<JournalLineSpec>> BuildJournalLinesAsync(
        OpeningBalanceDocument doc,
        CancellationToken cancellationToken)
    {
        var rate = doc.ExchangeRate <= 0 ? 1m : doc.ExchangeRate;
        var lines = new List<JournalLineSpec>();
        var narrative = $"رصيد افتتاحي {doc.Number}";

        foreach (var line in doc.Lines)
        {
            var amount = line.Amount * rate;
            if (amount <= 0) continue;

            switch (doc.Type)
            {
                case OpeningBalanceType.OpeningStock:
                    lines.Add(new(AccountingAccountIds.InventoryAsset, amount, 0, narrative, null));
                    lines.Add(new(AccountingAccountIds.OpeningBalanceEquity, 0, amount, narrative, null));
                    break;

                case OpeningBalanceType.CustomerReceivable:
                    lines.Add(new(AccountingAccountIds.AccountsReceivable, amount, 0, narrative, line.PartyId));
                    lines.Add(new(AccountingAccountIds.OpeningBalanceEquity, 0, amount, narrative, null));
                    break;

                case OpeningBalanceType.SupplierPayable:
                {
                    var payables = line.PartyId is Guid sid
                        ? await context.Suppliers.AsNoTracking()
                            .Where(s => s.Id == sid)
                            .Select(s => s.PayablesAccountId)
                            .FirstOrDefaultAsync(cancellationToken)
                        : AccountingAccountIds.AccountsPayable;
                    if (payables == Guid.Empty)
                        payables = AccountingAccountIds.AccountsPayable;
                    lines.Add(new(AccountingAccountIds.OpeningBalanceEquity, amount, 0, narrative, null));
                    lines.Add(new(payables, 0, amount, narrative, line.PartyId));
                    break;
                }

                case OpeningBalanceType.Cash:
                {
                    var cashAccount = await ResolveCashAccountIdAsync(line, cancellationToken);
                    lines.Add(new(cashAccount, amount, 0, narrative, null));
                    lines.Add(new(AccountingAccountIds.OpeningBalanceEquity, 0, amount, narrative, null));
                    break;
                }

                case OpeningBalanceType.Bank:
                {
                    var bankAccount = line.AccountId ?? AccountingAccountIds.CashUsd;
                    lines.Add(new(bankAccount, amount, 0, narrative, null));
                    lines.Add(new(AccountingAccountIds.OpeningBalanceEquity, 0, amount, narrative, null));
                    break;
                }

                case OpeningBalanceType.Capital:
                {
                    var funding = line.AccountId ?? AccountingAccountIds.CashUsd;
                    lines.Add(new(funding, amount, 0, narrative, null));
                    lines.Add(new(AccountingAccountIds.PartnerCapital, 0, amount, narrative, line.PartyId));
                    break;
                }

                case OpeningBalanceType.GeneralLedger:
                    if (line.Debit > 0)
                        lines.Add(new(line.AccountId ?? AccountingAccountIds.CashUsd, line.Debit * rate, 0, line.Description ?? narrative, line.PartyId));
                    if (line.Credit > 0)
                        lines.Add(new(line.AccountId ?? AccountingAccountIds.CashUsd, 0, line.Credit * rate, line.Description ?? narrative, line.PartyId));
                    break;

                default:
                    lines.Add(new(line.AccountId ?? AccountingAccountIds.CashUsd, amount, 0, narrative, line.PartyId));
                    lines.Add(new(AccountingAccountIds.OpeningBalanceEquity, 0, amount, narrative, null));
                    break;
            }
        }

        return ConsolidateJournalLines(lines);
    }

    private async Task<Guid> ResolveCashAccountIdAsync(OpeningBalanceLine line, CancellationToken cancellationToken)
    {
        if (line.AccountId is Guid cashboxId)
        {
            var accountId = await context.Cashboxes.AsNoTracking()
                .Where(c => c.Id == cashboxId)
                .Select(c => c.AccountId)
                .FirstOrDefaultAsync(cancellationToken);
            if (accountId is Guid aid && aid != Guid.Empty)
                return aid;
        }

        return AccountingAccountIds.CashUsd;
    }

    private static List<JournalLineSpec> ConsolidateJournalLines(List<JournalLineSpec> lines)
    {
        return lines
            .GroupBy(l => (l.AccountId, l.PartyId, l.Narrative))
            .Select(g => new JournalLineSpec(
                g.Key.AccountId,
                g.Sum(x => x.Debit),
                g.Sum(x => x.Credit),
                g.Key.Narrative,
                g.Key.PartyId))
            .Where(l => l.Debit > 0 || l.Credit > 0)
            .ToList();
    }

    private async Task ApplyPartyEffectsAsync(OpeningBalanceDocument doc, CancellationToken cancellationToken)
    {
        foreach (var line in doc.Lines)
        {
            if (doc.Type == OpeningBalanceType.CustomerReceivable && line.PartyId is Guid cid)
            {
                var agg = await customerRepository.GetByIdAsync(cid, cancellationToken);
                if (agg is not null)
                {
                    agg.Customer.MarkOpeningBalancePosted(line.Amount);
                    await customerRepository.UpdateAsync(agg, cancellationToken);
                }
            }

            if (doc.Type == OpeningBalanceType.SupplierPayable && line.PartyId is Guid sid)
            {
                var agg = await supplierRepository.GetByIdAsync(sid, cancellationToken);
                if (agg is not null)
                {
                    agg.Supplier.MarkOpeningBalancePosted(line.Amount);
                    await supplierRepository.UpdateAsync(agg, cancellationToken);
                }
            }
        }
    }

    private static void ValidateLine(
        OpeningBalanceType type,
        int row,
        OpeningBalanceLineInput input,
        OpeningBalanceLine line,
        OpeningBalanceLookupsDto lookup,
        List<OpeningBalanceValidationIssueDto> errors,
        List<OpeningBalanceValidationIssueDto> warnings)
    {
        switch (type)
        {
            case OpeningBalanceType.CustomerReceivable:
                if (string.IsNullOrWhiteSpace(input.PartyName) && input.PartyId is null)
                    errors.Add(new() { RowNumber = row, Field = "Customer", Message = "العميل مطلوب." });
                if (input.Debit < 0 || input.Credit < 0)
                    errors.Add(new() { RowNumber = row, Field = "Amount", Message = "لا يُسمح بقيم سالبة." });
                if (input.Debit <= 0 && input.Credit <= 0)
                    errors.Add(new() { RowNumber = row, Field = "Amount", Message = "المبلغ يجب أن يكون أكبر من صفر." });
                if (input.Debit > 0 && input.Credit > 0)
                    errors.Add(new() { RowNumber = row, Field = "Amount", Message = "أدخل مديناً أو دائناً — وليس كليهما." });
                break;

            case OpeningBalanceType.SupplierPayable:
                if (string.IsNullOrWhiteSpace(input.PartyName) && input.PartyId is null)
                    errors.Add(new() { RowNumber = row, Field = "Supplier", Message = "المورد مطلوب." });
                if (input.Debit <= 0 && input.Credit <= 0)
                    errors.Add(new() { RowNumber = row, Field = "Amount", Message = "المبلغ يجب أن يكون أكبر من صفر." });
                break;

            case OpeningBalanceType.OpeningStock:
                if (string.IsNullOrWhiteSpace(input.WarehouseName) && input.WarehouseId is null)
                    errors.Add(new() { RowNumber = row, Field = "Warehouse", Message = "المستودع مطلوب." });
                if (string.IsNullOrWhiteSpace(input.ItemName))
                    errors.Add(new() { RowNumber = row, Field = "Fabric", Message = "الصنف مطلوب." });
                if (string.IsNullOrWhiteSpace(input.ColorName))
                    errors.Add(new() { RowNumber = row, Field = "FabricColor", Message = "اللون مطلوب." });
                if ((input.Quantity ?? 0) <= 0)
                    warnings.Add(new() { RowNumber = row, Field = "Meters", Message = "الكمية صفر أو غير محددة.", IsWarning = true });
                break;

            case OpeningBalanceType.Cash:
                if (string.IsNullOrWhiteSpace(input.AccountName) && input.AccountId is null)
                    errors.Add(new() { RowNumber = row, Field = "CashAccount", Message = "حساب النقدية مطلوب." });
                break;

            case OpeningBalanceType.Bank:
                if (string.IsNullOrWhiteSpace(input.BankName))
                    warnings.Add(new() { RowNumber = row, Field = "Bank", Message = "اسم البنك غير محدد.", IsWarning = true });
                break;

            case OpeningBalanceType.GeneralLedger:
                if (string.IsNullOrWhiteSpace(input.AccountName) && input.AccountId is null)
                    errors.Add(new() { RowNumber = row, Field = "Account", Message = "الحساب مطلوب." });
                if (input.Debit <= 0 && input.Credit <= 0)
                    errors.Add(new() { RowNumber = row, Field = "Amount", Message = "يجب إدخال مدين أو دائن." });
                break;
        }

        if (!string.IsNullOrWhiteSpace(input.PartyName) && input.PartyId is null && type == OpeningBalanceType.CustomerReceivable)
        {
            var found = lookup.Customers.Any(c =>
                c.Name.Equals(input.PartyName, StringComparison.OrdinalIgnoreCase) ||
                c.Code.Equals(input.PartyName, StringComparison.OrdinalIgnoreCase));
            if (!found)
                errors.Add(new() { RowNumber = row, Field = "CustomerCode", Message = $"كود/اسم العميل غير معروف: {input.PartyName}" });
        }
    }

    private static OpeningBalanceValidationReportDto BuildReport(
        int total,
        int valid,
        int duplicates,
        List<OpeningBalanceValidationIssueDto> errors,
        List<OpeningBalanceValidationIssueDto> warnings) => new()
    {
        IsValid = errors.Count == 0,
        TotalRows = total,
        ValidRows = valid,
        DuplicateRows = duplicates,
        Errors = errors,
        Warnings = warnings
    };
}
