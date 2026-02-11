namespace OrderApprovalSystem.Models
{
    public class mHeadOrderDepartment : mApproval
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        public mHeadOrderDepartment()
        {
            LoadData();
        }

        #region Загрузка данных

        protected override IQueryable<TechnologicalOrder> BuildBaseQuery()
        {
            return from OrderApproval in db.mGetQuery<OrderApproval>()
                   join OrderApprovalDrafts in db.mGetQuery<OrderApprovalDrafts>() on OrderApproval.ID equals OrderApprovalDrafts.OrderApprovalID
                   join zayvka in db.mGetQuery<zayvka>() on OrderApproval.zayvkaID equals zayvka.id
                   join os_pro in db.mGetQuery<os_pro>() on zayvka.zak_1 equals os_pro.zak_1

                   // LEFT JOIN oborud
                   join oborud in db.mGetQuery<oborud>() on zayvka.rab_m equals oborud.rab_m into obGroup
                   from oborud in obGroup.DefaultIfEmpty()

                       // LEFT JOIN s_oper
                   join s_oper in db.mGetQuery<s_oper>() on zayvka.kodop equals s_oper.code into soGroup
                   from s_oper in soGroup.DefaultIfEmpty()

                       // LEFT JOIN prod
                   join p in db.mGetQuery<prod>()
                       on new { zayvka.zak_1, IsDrNull = (decimal?)null }
                       equals new { p.zak_1, IsDrNull = p.dr } into pGroup
                   from p in pGroup.DefaultIfEmpty()

                   where os_pro.d_vn_14 != null

                   orderby zayvka.zak_1

                   select new TechnologicalOrder
                   {
                       OrderNumber = zayvka.zak_1,
                       OrderApprovalID = OrderApproval.ID,
                       OrderApprovalDraftID = OrderApprovalDrafts.ID,
                       Technologist = OrderApproval.Technologist,
                       CoreDraft = (decimal)OrderApproval.CoreDraft,
                       Draft = OrderApproval.Draft,
                       CoreDraftName = OrderApproval.CoreDraftName.Trim(),
                       DraftName = OrderApproval.DraftName.Trim(),

                       Workshop = OrderApproval.Workshop,
                       Warehouse = OrderApproval.Warehouse,
                       Schedule = zayvka.graf,

                       Workplace = oborud != null ? (oborud.code + " " + oborud.oborud1).Trim() : null,
                       Operation = s_oper != null ? s_oper.oper.Trim() : null,

                       EquipmentDraft = OrderApprovalDrafts.EquipmentDraft,
                       EquipmentName = OrderApprovalDrafts.EquipmentName.Trim(),
                       EquipmentNameFromTechologist = OrderApproval.EquipmentNameFromTechnologist.Trim(),
                       EquipmentQuantityForOperation = OrderApproval.EquipmentQuantityForOperation,
                       EquimentRequiredQuantity = OrderApprovalDrafts.EquimentRequiredQuantity,
                       Cooperation = OrderApprovalDrafts.Cooperation,
                       IsDeletedFromOrder = OrderApprovalDrafts.IsDeletedFromOrder,

                       Note = zayvka.prim,
                       Analog = OrderApproval.Analog,
                       DesignComment = OrderApprovalDrafts.CommentForDesign,
                       ManufacturingComment = OrderApprovalDrafts.CommentForManufacturing,
                       OpenAtByTechnologist = OrderApproval.OpenAt,
                       Comment = null
                   };
        }

        #endregion Загрузка данных

        #region Методы согласования

        public Result ApproveOrder(Dictionary<string, object> data)
        {
            LoggerManager.MainLogger.Debug($"ApproveOrder for {CurrentItem.OrderNumber}");

            try
            {
                string comment = (string)data["Comment"];

                // Получение количества рабочих дней для согласования
                int workingDaysCount = db.mGetQuery<OrderApproval>()
                    .Where(order => order.ID == CurrentItem.OrderApprovalID)
                    .Join(
                        db.mGetQuery<OrderApprovalTypes>(),
                        order => order.EquipmentTypeID,
                        approvalType => approvalType.ID,
                        (order, approvalType) => approvalType.Term
                    )
                    .FirstOrDefault();

                // Получение даты выполнения с учетом рабочих дней
                DateTime deadlineDate = db.mGetQuery<calend>()
                    .Where(calendarDay => calendarDay.mday > DateTime.Today && calendarDay.v == true)
                    .OrderBy(calendarDay => calendarDay.mday)
                    .Skip(workingDaysCount - 1)
                    .Take(1)
                    .Select(calendarDay => calendarDay.mday)
                    .FirstOrDefault();

                // Если не нашли дату в календаре, используем простое сложение дней
                if (deadlineDate == default(DateTime))
                {
                    deadlineDate = DateTime.Today.AddDays(workingDaysCount);
                }

                // Обновление текущей записи согласования
                OrderApprovalHistory thisStepRecord = db.mGetList<OrderApprovalHistory>(
                    record =>
                    record.OrderApprovalID == CurrentItem.OrderApprovalID
                    && record.RecipientRole == "Начальник отдела заказов"
                    && record.RecipientName == "Дингес"
                )
                .Data
                .OrderByDescending(record => record.ID)
                .FirstOrDefault();

                if (thisStepRecord == null)
                {
                    return Result.Failed("Не найдена текущая запись согласования!");
                }

                thisStepRecord.CompletionDate = DateTime.Now;
                thisStepRecord.Status = "Выполнено";
                thisStepRecord.Result = "Согласовано";

                Result status = db.mUpdate(thisStepRecord);
                if (status.IsFailed)
                {
                    return Result.Failed("Не удалось обновить запись текущего согласования в БД!");
                }

                // Создание новой записи для следующего этапа согласования
                OrderApprovalHistory nextStepRecord = new OrderApprovalHistory
                {
                    OrderApprovalID = CurrentItem.OrderApprovalID,
                    ReceiptDate = DateTime.Now,
                    CompletionDate = null,
                    Term = deadlineDate,
                    RecipientRole = "Менеджер заказов",
                    RecipientName = "Папаева",
                    SenderRole = "Начальник отдела заказов",
                    SenderName = "Дингес",
                    Result = null,
                    Comment = comment
                };

                status = db.mAdd(nextStepRecord);
                if (status.IsFailed)
                {
                    return Result.Failed("Не удалось сохранить запись нового согласования в БД!");
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка в ApproveOrder: {ex.Message}");
                return Result.Failed($"Произошла ошибка при согласовании: {ex.Message}");
            }
        }

        public Result RejectOrder(Dictionary<string, object> data)
        {
            LoggerManager.MainLogger.Debug("Call RejectOrder");

            try
            {
                // Находим ID последней записи согласования
                var maxId = db.mGetQuery<OrderApprovalHistory>()
                    .Where(oah => oah.OrderApprovalID == CurrentItem.OrderApprovalID
                               && oah.RecipientRole == "Начальник отдела заказов"
                               && oah.RecipientName == "Дингес")
                    .Select(oah => oah.ID)
                    .Max();

                // Получаем последнюю запись согласования
                OrderApprovalHistory previousStepRecord = db.mGetSingle<OrderApprovalHistory>(
                    record => record.ID == maxId
                ).Data;

                if (previousStepRecord is null)
                {
                    LoggerManager.MainLogger.Error("Не найдена прошлая строка согласования");
                    return Result.Failed("Не найдена предыдущая запись согласования");
                }

                // Обновляем текущую запись согласования
                previousStepRecord.Status = "Выполнено";
                previousStepRecord.Result = "На доработку";
                previousStepRecord.CompletionDate = DateTime.Now;

                Result updateResult = db.mUpdate(previousStepRecord);
                if (updateResult.IsFailed)
                {
                    LoggerManager.MainLogger.Error($"Ошибка при обновлении записи: {updateResult.Message}");
                    return Result.Failed("Не удалось обновить запись согласования");
                }

                // Получение количества рабочих дней для согласования из OrderApprovalTypes
                int workingDaysCount = db.mGetQuery<OrderApproval>()
                    .Where(order => order.ID == CurrentItem.OrderApprovalID)
                    .Join(
                        db.mGetQuery<OrderApprovalTypes>(),
                        order => order.EquipmentTypeID,
                        approvalType => approvalType.ID,
                        (order, approvalType) => approvalType.Term
                    )
                    .FirstOrDefault();

                if (workingDaysCount <= 0)
                {
                    LoggerManager.MainLogger.Error($"Not found term for approvalType:");
                    return Result.Failed("Не найден срок");
                }

                // Расчет даты выполнения с учетом рабочих дней
                DateTime today = DateTime.Today;
                DateTime deadlineDate;

                // Получаем ближайшую рабочую дату после сегодняшней
                var firstWorkingDay = db.mGetQuery<calend>()
                    .Where(calendarDay => calendarDay.mday > today && calendarDay.v == true)
                    .OrderBy(calendarDay => calendarDay.mday)
                    .FirstOrDefault();

                if (firstWorkingDay != null)
                {
                    // Ищем дату, отстоящую на workingDaysCount рабочих дней вперед
                    deadlineDate = db.mGetQuery<calend>()
                        .Where(calendarDay => calendarDay.mday > today && calendarDay.v == true)
                        .OrderBy(calendarDay => calendarDay.mday)
                        .Skip(workingDaysCount - 1)
                        .Take(1)
                        .Select(calendarDay => calendarDay.mday)
                        .FirstOrDefault();
                }
                else
                {
                    deadlineDate = default(DateTime);
                }

                // Если не нашли дату в календаре, используем простое сложение дней
                if (deadlineDate == default(DateTime))
                {
                    // Добавляем рабочие дни + возможные выходные
                    deadlineDate = today.AddDays(workingDaysCount * 1.4);
                }

                // Извлечение данных из входного словаря
                string recipientRole = (string)data["Subdivision"];
                string recipientName = (string)data["SubdivisionRecipient"];
                string comment = (string)data["Comment"];

                // Создание новой записи для следующего этапа согласования
                OrderApprovalHistory nextStepRecord = new OrderApprovalHistory
                {
                    OrderApprovalID = CurrentItem.OrderApprovalID,
                    ReceiptDate = DateTime.Now,
                    CompletionDate = null,
                    Term = deadlineDate,
                    RecipientRole = recipientRole,
                    RecipientName = recipientName,
                    SenderRole = "Начальник отдела заказов",
                    SenderName = "Дингес",
                    Status = "В работе",
                    Result = null,
                    Comment = comment
                };

                Result addResult = db.mAdd(nextStepRecord);
                if (addResult.IsFailed)
                {
                    LoggerManager.MainLogger.Error($"Ошибка при добавлении записи: {addResult.Message}");
                    return Result.Failed("Не удалось создать новую запись согласования");
                }

                LoggerManager.MainLogger.Info($"Заказ отклонен и перенаправлен в {recipientRole} - {recipientName}");
                return Result.Success();
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка в RejectOrder: {ex.Message}", ex);
                return Result.Failed($"Произошла ошибка при отклонении заказа: {ex.Message}");
            }
        }

        #endregion Методы согласования
    }
}
