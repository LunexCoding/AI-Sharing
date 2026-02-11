namespace OrderApprovalSystem.Models
{
    public abstract class mApproval : MBase
    {
        protected Fox.DatabaseService.IDatabaseService db = ServiceLocator.DatabaseService;

        #region Свойства

        private ObservableCollection<TechnologistGroup> _groupedData;
        public ObservableCollection<TechnologistGroup> GroupedData
        {
            get => _groupedData;
            set
            {
                _groupedData = value;
                OnPropertyChanged(nameof(GroupedData));
                OnPropertyChanged(nameof(TotalGroups));

                if (_groupedData != null && _groupedData.Any())
                {
                    CurrentGroup = _groupedData.First();
                }
            }
        }

        private TechnologistGroup _currentGroup;
        public TechnologistGroup CurrentGroup
        {
            get => _currentGroup;
            set
            {
                _currentGroup = value;
                OnPropertyChanged(nameof(CurrentGroup));

                if (_groupedData != null && _currentGroup != null)
                {
                    CurrentGroupIndex = _groupedData.IndexOf(_currentGroup);
                }

                if (_currentGroup != null && _currentGroup.Items.Any())
                {
                    CurrentItem = _currentGroup.Items.First();
                    CurrentIndex = 0;
                }
                else
                {
                    CurrentItem = null;
                    CurrentIndex = 0;
                }
            }
        }

        private TechnologicalOrder _currentItem;
        public TechnologicalOrder CurrentItem
        {
            get => _currentItem;
            set
            {
                _currentItem = value;
                OnPropertyChanged(nameof(CurrentItem));

                if (_currentGroup != null && _currentItem != null && _currentGroup.Items.Contains(_currentItem))
                {
                    CurrentIndex = _currentGroup.Items.IndexOf(_currentItem);
                }
            }
        }

        private int _currentGroupIndex;
        public int CurrentGroupIndex
        {
            get => _currentGroupIndex;
            set
            {
                _currentGroupIndex = value;
                OnPropertyChanged(nameof(CurrentGroupIndex));
            }
        }

        private int _currentIndex;
        public int CurrentIndex
        {
            get => _currentIndex;
            set
            {
                _currentIndex = value;
                OnPropertyChanged(nameof(CurrentIndex));
            }
        }

        public bool HasPrevious => CurrentGroup != null && CurrentIndex > 0;
        public bool HasNext => CurrentGroup != null && CurrentIndex < CurrentGroup.Items.Count - 1;
        public bool HasPreviousGroup => GroupedData != null && CurrentGroupIndex > 0;
        public bool HasNextGroup => GroupedData != null && CurrentGroupIndex < GroupedData.Count - 1;
        public int TotalGroups => GroupedData?.Count ?? 0;

        #endregion

        #region Методы навигации

        public void NavigatePrevious()
        {
            if (HasPrevious)
            {
                CurrentIndex--;
                CurrentItem = CurrentGroup.Items[CurrentIndex];
            }
        }

        public void NavigateNext()
        {
            if (HasNext)
            {
                CurrentIndex++;
                CurrentItem = CurrentGroup.Items[CurrentIndex];
            }
        }

        public void NavigateNextGroup()
        {
            if (HasNextGroup)
            {
                CurrentGroup = GroupedData[CurrentGroupIndex + 1];
            }
        }

        public void NavigatePreviousGroup()
        {
            if (HasPreviousGroup)
            {
                CurrentGroup = GroupedData[CurrentGroupIndex - 1];
            }
        }

        public void FindAndNavigate(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText) || GroupedData == null)
                return;

            string query = searchText.ToLower().Trim();

            var targetGroup = GroupedData.FirstOrDefault(g =>
                g.Zak1 != null && g.Zak1.ToLower().Contains(query));

            if (targetGroup != null && CurrentGroup != targetGroup)
            {
                CurrentGroup = targetGroup;
            }
        }

        #endregion

        #region Абстрактные методы

        protected abstract IQueryable<TechnologicalOrder> BuildBaseQuery();

        #endregion

        #region Загрузка данных

        protected virtual void LoadData()
        {
            try
            {
                var query = BuildBaseQuery();
                List<TechnologicalOrder> data = query.Distinct().ToList();

                GroupedData = new ObservableCollection<TechnologistGroup>(
                    data
                    .OrderBy(x => x.EquipmentDraft)
                    .GroupBy(x => x.OrderNumber)
                    .Select(g => new TechnologistGroup
                    {
                        Zak1 = g.Key,
                        Items = new ObservableCollection<TechnologicalOrder>(
                            g.OrderBy(x => x.OpenAtByTechnologist).ToList())
                    })
                    .OrderBy(g => g.Zak1)
                    .ToList()
                );
            }
            catch (Exception ex)
            {
                LoggerManager.MainLogger.Error($"Error loading data: {ex.Message}");
                GroupedData = new ObservableCollection<TechnologistGroup>();
            }
        }

        #endregion
    }
}
