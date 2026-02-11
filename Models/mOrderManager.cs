namespace OrderApprovalSystem.Models
{
    public class mOrderManager : mApproval
    {
        #region Свойства для UI

        private ObservableCollection<string> _nomenclatureGroups;
        public ObservableCollection<string> NomenclatureGroups
        {
            get => _nomenclatureGroups;
            set
            {
                _nomenclatureGroups = value;
                OnPropertyChanged(nameof(NomenclatureGroups));
            }
        }

        private ObservableCollection<OrderApprovalTypes> _equipmentTypes;
        public ObservableCollection<OrderApprovalTypes> EquipmentTypes
        {
            get => _equipmentTypes;
            set
            {
                _equipmentTypes = value;
                OnPropertyChanged(nameof(EquipmentTypes));
            }
        }

        #endregion Свойства для UI

        #region Конструктор

        public mOrderManager()
        {
            // Инициализация данных для выпадающих списков
            NomenclatureGroups = new ObservableCollection<string> { "Первая", "Вторая", "Третья" };
            EquipmentTypes = new ObservableCollection<OrderApprovalTypes>(
                db.mGetQuery<OrderApprovalTypes>().ToList()
            );

            LoadData();
        }

        #endregion Конструктор

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
                       Comment = null,

                       IsByMemo = OrderApproval.IsByMemo,
                       MemoNumber = OrderApproval.MemoNumber,
                       MemoAuthor = OrderApproval.MemoAuthor,
                       CoreOrderByMemo = OrderApproval.CoreOrder,
                       CoreNumberByMemo = OrderApproval.CoreNumber,
                       OrderByMemo = OrderApproval.Order,
                       NumberByMemo = OrderApproval.Number,
                       OrderName = OrderApproval.OrderName,
                       OpenAtByMemo = OrderApproval.OpenAtByMemo,
                       NomenclatureGroup = OrderApproval.NomenclatureGroup,
                       EquipmentTypeID = OrderApproval.EquipmentTypeID,
                       DraftByMemo = OrderApproval.DraftByMemo,
                       DraftNameByMemo = OrderApproval.DraftNameByMemo,
                       Balance = OrderApproval.Balance,
                       WorkshopByMemo = OrderApproval.WorkshopByMemo,
                       EquipmentRequiredQuantityByMemo = OrderApproval.EquipmentRequiredQuantityByMemo
                   };
        }

        protected override void LoadData()
        {
            try
            {
                base.LoadData();

                // Обновляем EquipmentType для каждого заказа
                foreach (var group in GroupedData)
                {
                    foreach (var order in group.Items)
                    {
                        order.EquipmentType = ConvertToOrderApprovalType(order.EquipmentTypeID);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Error loading data: {ex.Message}");
                GroupedData = new ObservableCollection<TechnologistGroup>();
            }
        }

        private OrderApprovalTypes ConvertToOrderApprovalType(int? typeId)
        {
            if (typeId.HasValue)
            {
                return db.mGetSingle<OrderApprovalTypes>(
                    record => record.ID == typeId.Value
                ).Data;
            }
            return null;
        }

        #endregion Загрузка данных

        #region Методы согласования

        public Result ApproveOrder()
        {
            if (CurrentItem.IsByMemo)
            {
                return ApproveOrderByMemo();
            }
            return ApproveTechnologicalOrder();
        }

        private Result ApproveOrderByMemo()
        {
            // Валидация данных
            var validationResult = CreateOrderByMemoValidator.Validate(CurrentItem);
            if (validationResult.IsFailed)
            {
                return validationResult;
            }

            try
            {
                // Создаем новую запись OrderApproval для СЗ
                var newOrder = new OrderApproval
                {
                    IsByMemo = true,
                    MemoNumber = CurrentItem.MemoNumber,
                    MemoAuthor = CurrentItem.MemoAuthor,
                    Order = CurrentItem.OrderByMemo,
                    Number = CurrentItem.NumberByMemo,
                    OrderName = CurrentItem.OrderName,
                    CoreOrder = CurrentItem.CoreOrderByMemo,
                    CoreNumber = CurrentItem.CoreNumberByMemo,
                    OpenAtByMemo = CurrentItem.OpenAtByMemo,
                    NomenclatureGroup = CurrentItem.NomenclatureGroup,
                    EquipmentTypeID = CurrentItem.EquipmentType?.ID,
                    DraftByMemo = CurrentItem.DraftByMemo,
                    DraftNameByMemo = CurrentItem.DraftNameByMemo,
                    Balance = CurrentItem.Balance,
                    WorkshopByMemo = CurrentItem.WorkshopByMemo,
                    EquipmentRequiredQuantityByMemo = CurrentItem.EquipmentRequiredQuantityByMemo
                };

                var addResult = db.mAdd(newOrder);
                if (addResult.IsFailed)
                {
                    return Result.Failed("Не удалось создать заказ по СЗ");
                }

                db.mSaveChanges();

                // Получаем созданную запись
                var addedRecord = db.mGetQuery<OrderApproval>()
                    .OrderByDescending(r => r.ID)
                    .FirstOrDefault();

                // Создаем запись в истории согласования
                var historyRecord = new OrderApprovalHistory
                {
                    OrderApprovalID = addedRecord.ID,
                    ReceiptDate = DateTime.Now,
                    CompletionDate = DateTime.Now,
                    Term = DateTime.Now.Date.AddDays(1),
                    RecipientRole = "дальше",
                    RecipientName = "кто-то",
                    SenderRole = "Менеджер заказов",
                    SenderName = "Папаева",
                    Status = "Выполнено",
                    Result = "Согласовано"
                };

                db.mAdd(historyRecord);
                db.mSaveChanges();

                return Result.Success();
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка при создании заказа по СЗ: {ex.Message}");
                return Result.Failed($"Ошибка при создании заказа: {ex.Message}");
            }
        }

        private Result ApproveTechnologicalOrder()
        {
            // Валидация данных
            var validationResult = CreateOrderByMemoValidator.Validate(CurrentItem);
            if (validationResult.IsFailed)
            {
                return validationResult;
            }

            try
            {
                // Обновляем существующий OrderApproval
                var orderRecord = db.mGetSingle<OrderApproval>(
                    r => r.ID == CurrentItem.OrderApprovalID
                ).Data;

                if (orderRecord == null)
                {
                    return Result.Failed("Не найден заказ для обновления");
                }

                orderRecord.Order = CurrentItem.OrderByMemo;
                orderRecord.Number = CurrentItem.NumberByMemo;
                orderRecord.OrderName = CurrentItem.OrderName;
                orderRecord.CoreOrder = CurrentItem.CoreOrderByMemo;
                orderRecord.CoreNumber = CurrentItem.CoreNumberByMemo;
                orderRecord.OpenAtByMemo = CurrentItem.OpenAtByMemo;
                orderRecord.NomenclatureGroup = CurrentItem.NomenclatureGroup;
                orderRecord.EquipmentTypeID = CurrentItem.EquipmentType?.ID;
                orderRecord.DraftByMemo = CurrentItem.DraftByMemo;
                orderRecord.DraftNameByMemo = CurrentItem.DraftNameByMemo;
                orderRecord.Balance = CurrentItem.Balance;
                orderRecord.WorkshopByMemo = CurrentItem.WorkshopByMemo;
                orderRecord.EquipmentRequiredQuantityByMemo = CurrentItem.EquipmentRequiredQuantityByMemo;

                var updateResult = db.mUpdate(orderRecord);
                if (updateResult.IsFailed)
                {
                    return Result.Failed("Не удалось обновить данные заказа");
                }

                // Обновляем текущую запись в истории согласования
                var currentHistory = db.mGetList<OrderApprovalHistory>(
                    r => r.OrderApprovalID == orderRecord.ID
                ).Data
                .OrderByDescending(r => r.ID)
                .FirstOrDefault();

                if (currentHistory != null)
                {
                    currentHistory.CompletionDate = DateTime.Now;
                    currentHistory.Status = "Выполнено";
                    currentHistory.Result = "Согласовано";
                    db.mUpdate(currentHistory);
                }

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

                if (deadlineDate == default(DateTime))
                {
                    deadlineDate = DateTime.Today.AddDays(workingDaysCount);
                }

                // Создаем новую запись для следующего этапа
                var nextHistory = new OrderApprovalHistory
                {
                    OrderApprovalID = orderRecord.ID,
                    ReceiptDate = DateTime.Now,
                    CompletionDate = null,
                    Term = deadlineDate,
                    RecipientRole = "Кто-то после менеджера",
                    RecipientName = "Кто-то после менеджера",
                    SenderRole = "Менеджер заказов",
                    SenderName = "Папаева",
                    Status = "В работе",
                    Result = null
                };

                db.mAdd(nextHistory);
                db.mSaveChanges();

                return Result.Success();
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка при согласовании тех. заказа: {ex.Message}");
                return Result.Failed($"Ошибка при согласовании: {ex.Message}");
            }
        }

        public Result RejectOrder(Dictionary<string, object> data)
        {
            LoggerManager.MainLogger.Debug("Call RejectOrder");

            try
            {
                // Находим ID последней записи согласования для менеджера Папаевой
                var maxId = db.mGetQuery<OrderApprovalHistory>()
                    .Where(oah => oah.OrderApprovalID == CurrentItem.OrderApprovalID
                               && oah.RecipientRole == "Менеджер заказов"
                               && oah.RecipientName == "Папаева")
                    .Select(oah => oah.ID)
                    .Max();

                // Получаем последнюю запись согласования
                var previousStepRecord = db.mGetSingle<OrderApprovalHistory>(
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

                var updateResult = db.mUpdate(previousStepRecord);
                if (updateResult.IsFailed)
                {
                    LoggerManager.MainLogger.Error($"Ошибка при обновлении записи: {updateResult.Message}");
                    return Result.Failed("Не удалось обновить запись согласования");
                }

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

                if (workingDaysCount <= 0)
                {
                    return Result.Failed("Не найден срок для назначения согласования!");
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
                    return Result.Failed("Ошибка установки строки согласования");
                }

                if (deadlineDate == default(DateTime))
                {
                    deadlineDate = today.AddDays(workingDaysCount * 1.4);
                }

                // Извлечение данных из входного словаря
                string recipientRole = (string)data["Subdivision"];
                string recipientName = (string)data["SubdivisionRecipient"];
                string comment = (string)data["Comment"];

                // Создание новой записи для следующего этапа согласования
                var nextStepRecord = new OrderApprovalHistory
                {
                    OrderApprovalID = CurrentItem.OrderApprovalID,
                    ReceiptDate = DateTime.Now,
                    CompletionDate = null,
                    Term = deadlineDate,
                    RecipientRole = recipientRole,
                    RecipientName = recipientName,
                    SenderRole = "Менеджер заказов",
                    SenderName = "Папаева",
                    Status = "В работе",
                    Result = null,
                    Comment = comment
                };

                var addResult = db.mAdd(nextStepRecord);
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

        #region Методы для UI

        public Result CreateOrderByMemo()
        {
            try
            {
                var newGroup = new TechnologistGroup
                {
                    Zak1 = "Новая СЗ",
                    IsByMemo = true,
                    Items = new ObservableCollection<TechnologicalOrder>
                    {
                        new TechnologicalOrder
                        {
                            IsByMemo = true,
                            MemoNumber = "Новая",
                            OpenAtByMemo = DateTime.Now
                        }
                    }
                };

                if (GroupedData == null)
                {
                    GroupedData = new ObservableCollection<TechnologistGroup>();
                }

                GroupedData.Add(newGroup);
                CurrentGroup = newGroup;

                return Result.Success();
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Ошибка при создании заказа по СЗ: {ex.Message}");
                return Result.Failed($"Ошибка: {ex.Message}");
            }
        }

        #endregion Методы для UI
    }
}
