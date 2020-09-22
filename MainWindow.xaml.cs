using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ASCON_TestApp
{
    public partial class MainWindow : Window
    {
        private DataClasses1DataContext db;
        private IQueryable componentsAllQuery;
        private IQueryable componentsUniqueQuery;

        public MainWindow()
        {
            InitializeComponent();

            // Подключение к БД
            db = new DataClasses1DataContext();

            // Получение объекта IQueryable из базы данных
            componentsUniqueQuery = (from cl in db.ComponentsUniques where cl is ComponentsUnique select cl);

            // Получение объекта IQueryable из базы данных
            componentsAllQuery = (from cl in db.ComponentsAlls where cl is ComponentsAll select cl);

            // Первоначальное заполнение treeView
            treeView_Expanded(null, new RoutedEventArgs());
        }

        // Handler при раскрытии ветки
        private void treeView_Expanded(object sender, RoutedEventArgs e)
        {
            // Получение объекта treeView
            TreeViewItem item;
            try
            {
                // Раскрытие определенного узла
                item = (TreeViewItem)e.OriginalSource;
            }
            // Если узел не передан через RoutedEventArgs - первоначальное раскрытие дерева
            catch (NullReferenceException)
            {
                item = null;
            }

            // Первоначальное заполнение TreeView
            if (item == null)
            {
                treeView.Items.Clear();

                // Для всех компонентов, у которых id родителя == 0
                foreach (ComponentsAll component in componentsAllQuery.AsQueryable().Cast<ComponentsAll>()
                                                                      .Where(x => x.ParentId == 0))
                {
                    // Создание нового объекта TreeViewItem (корневого)
                    TreeViewItem rootItem = new TreeViewItem();

                    // Присваивание тега к объекту TreeView как id текущего компонента
                    rootItem.Tag = component.Id;

                    // Текст объекта  TreeView как поле Name текущего компонента
                    rootItem.Header = component.Name.ToString();

                    // Если у данного компонента есть дочерние компоненты
                    if (componentsAllQuery.AsQueryable().Cast<ComponentsAll>()
                                                        .Where(x => x.ParentId == component.Id).ToList().Count > 0)
                    {
                        // Добавление дочернего элемента к новому для возможности его последующего раскрытия и запуска обработчика treeView_Expanded
                        rootItem.Items.Add("*");
                    }

                    treeView.Items.Add(rootItem);

                    // Сортировка элементов по алфавиту
                    rootItem.Items.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));
                }
            }
            // Раскрытие какого-либо узла
            else
            {
                item.Items.Clear();

                try
                {
                    // Для всех дочерних компонентов данного компонента
                    foreach (ComponentsAll component in componentsAllQuery.AsQueryable().Cast<ComponentsAll>()
                                                                          .Where(x => x.ParentId == (long)item.Tag))
                    {
                        TreeViewItem newitem = CreateNewItemFromComponent(component);

                        // Если у данного дочернего компонента есть другие дочерние компоненты
                        if (componentsAllQuery.AsQueryable().Cast<ComponentsAll>()
                                                            .Where(x => x.ParentId == component.Id).ToList().Count > 0)
                        {
                            // Добавление элемента для возможности его последующего раскрытия и запуска обработчика treeView_Expanded
                            newitem.Items.Add("*");
                        }

                        // Скрытие нового элемента
                        newitem.IsExpanded = false;

                        item.Items.Add(newitem);

                        // Сортировка элементов по алфавиту
                        item.Items.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));
                    }
                }
                catch
                {
                }
                finally
                {
                }
            }
        }

        // Добавление дочернего компонента
        private void NewInsideComponent(object sender, RoutedEventArgs e)
        {
            // Получение выбранного узла дерева
            TreeViewItem selectedItem = (TreeViewItem)treeView.SelectedItem;

            // Получение компонента по тегу данного узла
            ComponentsAll currentComponent = componentsAllQuery.AsQueryable().Cast<ComponentsAll>().Where(x => x.Id == (long)selectedItem.Tag).First();

            // Получение списка ID родителей данного компонента - нужно для последующего обновления дерева
            List<long> parensIDListUnique = new List<long>();
            parensIDListUnique.Add(currentComponent.ComponentId);
            parensIDListUnique = GetComponentsParentsIds(currentComponent, ref parensIDListUnique, true);

            // Лист Id родительских компонентов данного компонента
            List<long> parensIDListAll = new List<long>();
            parensIDListAll.Add(currentComponent.Id);
            parensIDListAll = GetComponentsParentsIds(currentComponent, ref parensIDListAll, false).OrderBy(u => u).ToList();

            // Создание диалога с добавлением нового компонента
            // Индекс компонента должен быть больше, чем все предыдущие, во избежание перезаписи таблицы
            // Id нового компонента будет задано автоматически при db.ComponentsUniques.InsertOnSubmit(...)
            Dialog_NewComponent dialog_NewComponent = new Dialog_NewComponent(componentsUniqueQuery, parensIDListUnique);
            dialog_NewComponent.Owner = Window.GetWindow(this);
            dialog_NewComponent.ShowDialog();

            // Получение максимального id в обеих таблицах - Индекс компонента должен быть больше, чем все предыдущие, во избежание перезаписи таблицы
            long currentAllComponentsMaxID = componentsAllQuery.AsQueryable().Cast<ComponentsAll>().Max(t => t.Id);
            long currentUniqueComponentsMaxID = componentsUniqueQuery.AsQueryable().Cast<ComponentsUnique>().Max(t => t.Id);

            // Объект из диалога существует (нажата кнопка ОК)
            if (dialog_NewComponent.newComponent != null && dialog_NewComponent.newComponent.Name != null)
            {
                // Уникальный компонент еще не существует
                if (dialog_NewComponent.newComponent.Id > currentUniqueComponentsMaxID)
                {
                    // Добавление нового уникального компонента
                    ComponentsUnique newComponentUnique = new ComponentsUnique()
                    {
                        // Индекс компонента должен быть больше, чем все предыдущие, во избежание перезаписи таблицы
                        Id = dialog_NewComponent.newComponent.Id,
                        Name = dialog_NewComponent.newComponent.Name,
                    };

                    // Сохранение изменений в БД
                    db.ComponentsUniques.InsertOnSubmit(newComponentUnique);
                    db.SubmitChanges();

                    // Создание нового компонента таблицы ComponentsAll
                    ComponentsAll newComponentAll = new ComponentsAll()
                    {
                        // Индекс компонента должен быть больше, чем все предыдущие, во избежание перезаписи таблицы
                        Id = currentAllComponentsMaxID + 1,
                        ParentId = (long)selectedItem.Tag,
                        // Нахождение Id уникального компонента по имени
                        ComponentId = componentsUniqueQuery.AsQueryable().Cast<ComponentsUnique>()
                                                           .Where(x => x.Name == newComponentUnique.Name).First().Id,
                        Name = "",
                        Amount = dialog_NewComponent.newComponentAmount
                    };

                    // Сохранение изменений в БД
                    db.ComponentsAlls.InsertOnSubmit(newComponentAll);
                    db.SubmitChanges();

                    // Добавление нового элемента TreeViewItem по новому компоненту
                    TreeViewItem newitem = CreateNewItemFromComponent(newComponentAll);

                    // Добавление нового элемента TreeViewItem в treeView под выбранным узлом selectedItem
                    selectedItem.Items.Add(newitem);
                }

                // Уникальный компонент уже существует
                else
                {
                    // Список Id внутренних уникальных компонентов данного компонета
                    List<long> childIDList = GetComponentsUniqueChildrensId(currentComponent);

                    // Изменение количества уже добавленного компонента
                    // Если Id любого внутреннего уникальных компонента равен id добавляемого уникального компонента
                    if (childIDList.Any(x => x == dialog_NewComponent.newComponent.Id))
                    {
                        // Выбор компонента, в котором нужно изменить значение
                        // Добавление нового значения  dialog_NewComponent.newComponentAmount к существующему Amount
                        componentsAllQuery.AsQueryable().Cast<ComponentsAll>()
                            .Where(x => x.ComponentId == dialog_NewComponent.newComponent.Id)
                            .Where(x => x.ParentId == (long)selectedItem.Tag)
                            .First().Amount += dialog_NewComponent.newComponentAmount;
                    }

                    // Данный уникальный компонент отсутствует в ветке
                    // Добавление существующего уникального компонента в текущую ветку
                    else
                    {
                        List<string> parentNames = GetComponentsParentsNames(parensIDListAll);

                        if (parentNames.Contains(dialog_NewComponent.newComponent.Name))
                        {
                            // Вывод предуреждения о переименовании
                            string messageBoxText = "Компонент \"" + dialog_NewComponent.newComponent.Name + " \" уже присутствует в данной ветке.\nДобавление невозможно.";
                            string caption = "Добавление компонента невозможно";
                            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Question);
                        }
                        else
                        {
                            ComponentsAll newComponentAll = new ComponentsAll()
                            {
                                Id = currentAllComponentsMaxID + 1,
                                ParentId = (long)selectedItem.Tag,
                                ComponentId = dialog_NewComponent.newComponent.Id,
                                Name = "",
                                Amount = dialog_NewComponent.newComponentAmount
                            };

                            // Сохранение изменений в БД
                            db.ComponentsAlls.InsertOnSubmit(newComponentAll);
                            db.SubmitChanges();

                            // Создание нового элемента TreeViewItem по текущему компоненту
                            TreeViewItem newitem = CreateNewItemFromComponent(newComponentAll);

                            // Добавление нового элемента TreeViewItem в treeView под выбранным узлом selectedItem
                            selectedItem.Items.Add(newitem);
                        }
                    }
                }

                // Сохранение изменений в БД. Мало ли.
                db.SubmitChanges();

                // Сворачивание TreeView
                TreeViewCollapseAll();

                // Обновление treeView - разворачивание узлов из списка parensIDListAll
                RefreshTreeView(parensIDListAll);
            }
        }

        // Переименование компонента
        private void RenameComponent(object sender, RoutedEventArgs e)
        {
            // Получение выбранного узла дерева
            TreeViewItem selectedItem = (TreeViewItem)treeView.SelectedItem;

            // Получение компонента по тегу данного узла
            ComponentsAll currentComponent = componentsAllQuery.AsQueryable().Cast<ComponentsAll>().Where(x => x.Id == (long)selectedItem.Tag).First();

            // Создание диалога с изменением компонента
            Dialog_RenameComponent dialog_RenameComponent = new Dialog_RenameComponent(componentsUniqueQuery, currentComponent);
            dialog_RenameComponent.Owner = Window.GetWindow(this);
            dialog_RenameComponent.ShowDialog();

            // Объект из диалога существует
            if (dialog_RenameComponent.componentNewName != null)
            {
                // Дочерний элемент treeView
                if (currentComponent.ParentId != 0)
                {
                    // Нахождение уникального компонента по ComponentId текущего компонента и замена в нем имени
                    componentsUniqueQuery.AsQueryable().Cast<ComponentsUnique>()
                                                   .Where(x => x.Id == currentComponent.ComponentId).First().Name = dialog_RenameComponent.componentNewName;
                }
                // Корневой элемент treeView
                else
                {
                    // Нахождение компонента по старому имени и замена в нем имени
                    componentsAllQuery.AsQueryable().Cast<ComponentsAll>()
                                      .Where(x => x.Name == dialog_RenameComponent.componentOldName).First().Name = dialog_RenameComponent.componentNewName;
                }
            }

            // Сохранение изменений в БД
            db.SubmitChanges();

            // Сворачивание TreeView
            TreeViewCollapseAll();

            // Лист Id родительских компонентов данного компонента
            List<long> parensIDList = new List<long>();
            parensIDList = GetComponentsParentsIds(currentComponent, ref parensIDList, false).OrderBy(u => u).ToList();
            // Обновление treeView с раскрытием до текущего компонента
            RefreshTreeView(parensIDList);
        }

        // Удаление компонента
        private void DeleteComponent(object sender, RoutedEventArgs e)
        {
            // Получение выбранного узла дерева
            TreeViewItem selectedItem = (TreeViewItem)treeView.SelectedItem;

            // Получение компонента по тегу данного узла
            ComponentsAll currentComponent = componentsAllQuery.AsQueryable().Cast<ComponentsAll>().Where(x => x.Id == (long)selectedItem.Tag).First();

            // Лист Id родительских компонентов данного компонента
            List<long> parensIDList = new List<long>();
            parensIDList = GetComponentsParentsIds(currentComponent, ref parensIDList, false).OrderBy(u => u).ToList();

            // Список Id внутренних уникальных компонентов данного компонета, все уровни
            List<long> childIDList = new List<long>();
            GetComponentsAllChildrensId(currentComponent, ref childIDList);

            // Если у данного компонента есть вложенные компоненты
            if (childIDList.Count != 0)
            {
                // Вывод предуреждения о переименовании
                string messageBoxText = "Удалить компонент \"" + selectedItem.Header + " \"? \nУдаление данного компонента приведет за собой удаление следующих компонентов:";
                // Вывод в предупреждении список всех дочерних элементов, которые будут удалены
                foreach (long id in childIDList)
                {
                    messageBoxText += "\n   - " + GetComponentUniquebyAllID(id).Name;
                }
                string caption = "Удаление компонента";
                MessageBoxResult result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.OKCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.OK)
                {
                    // Удаление всех дочерних компонентов из БД
                    foreach (long id in childIDList)
                    {
                        db.ComponentsAlls.DeleteOnSubmit(GetComponentAllByID(id));
                    }
                    // Удаление данного компонента из БД
                    db.ComponentsAlls.DeleteOnSubmit(currentComponent);
                }
                if (result == MessageBoxResult.Cancel)
                {
                }
            }
            else
            {
                // Вывод предуреждения о переименовании
                string messageBoxText = "Удалить компонент \"" + selectedItem.Header + " \"? ";
                string caption = "Удаление компонента";
                MessageBoxResult result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.OKCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.OK)
                {
                    // Удаление данного компонента из БД
                    db.ComponentsAlls.DeleteOnSubmit(currentComponent);
                }
                if (result == MessageBoxResult.Cancel)
                {
                }
            }

            // Сохранение изменений в БД
            db.SubmitChanges();

            // Сворачивание TreeView
            TreeViewCollapseAll();

            // Обновление treeView с раскрытием до текущего компонента
            RefreshTreeView(parensIDList);
        }

        // Добавление коренного компонента
        private void NewRootComponent(object sender, RoutedEventArgs e)
        {
            // Использование диалога при переименовании файла для создание нового корневого компонента - используя другой конструктор класса
            Dialog_RenameComponent dialog_NewRootComponent = new Dialog_RenameComponent(componentsAllQuery);
            dialog_NewRootComponent.Owner = Window.GetWindow(this);
            dialog_NewRootComponent.ShowDialog();

            if (dialog_NewRootComponent.componentNewName != null)
            {
                // Получение максимального id в таблице
                // Индекс компонента должен быть больше, чем все предыдущие, во избежание перезаписи таблицы
                // Id нового компонента будет задано автоматически при db.ComponentsUniques.InsertOnSubmit(...)
                long currentAllComponentsMaxID = componentsAllQuery.AsQueryable().Cast<ComponentsAll>().Max(t => t.Id);

                // Создание нового компонента таблицы ComponentsAll
                ComponentsAll newComponentAll = new ComponentsAll()
                {
                    // Индекс компонента должен быть больше, чем все предыдущие, во избежание перезаписи таблицы
                    Id = currentAllComponentsMaxID + 1,
                    ParentId = 0,
                    ComponentId = 0,
                    Name = dialog_NewRootComponent.componentNewName,
                    Amount = 1
                };

                // Сохранение изменений в БД
                db.ComponentsAlls.InsertOnSubmit(newComponentAll);
                db.SubmitChanges();

                // Добавление нового элемента TreeViewItem по новому компоненту
                TreeViewItem newitem = CreateNewItemFromComponent(newComponentAll);

                // Добавление нового элемента TreeViewItem в корень treeView
                treeView.Items.Add(newitem);
            }

            // Сохранение изменений в БД
            db.SubmitChanges();

            // Сворачивание TreeView
            TreeViewCollapseAll();

            // Разворачивание компонентов первого уровня
            foreach (TreeViewItem item in treeView.Items)
            {
                item.IsExpanded = true;
            }
        }

        // Создание отчета о сводном составе для компонента
        private void ReportComponent(object sender, RoutedEventArgs e)
        {
            // Получение выбранного узла дерева
            TreeViewItem selectedItem = (TreeViewItem)treeView.SelectedItem;

            // Получение компонента по тегу данного узла
            ComponentsAll currentComponent = componentsAllQuery.AsQueryable().Cast<ComponentsAll>().Where(x => x.Id == (long)selectedItem.Tag).First();

            // Список Id внутренних уникальных компонентов данного компонета, все уровни
            List<long> childIDList = new List<long>();
            GetComponentsAllChildrensId(currentComponent, ref childIDList);

            // Если у выбранного компонента нет дочерних компонентов
            if (childIDList.Count == 0)
            {
                string messageBoxText = "Данный компонент не содержит дочерних компонентов.\nСоздание отчета о сводном составе невозможно.";
                string caption = "Данный компонент не содержит дочерних компонентов";
                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // Вызов конструктора класса с экспортом в Word вложенных компонентов текущего компонента
            else
            {
                ExportToWord export = new ExportToWord(currentComponent, componentsAllQuery, componentsUniqueQuery, childIDList);
            }
        }

        // Обновление treeView
        // parensIDList - список Id родителей данного компонента
        private void RefreshTreeView(List<long> parensIDList)
        {
            // parentLevel показывет уровень вложенности компонента в дереве.
            int parentLevel = 0;

            // Обновление дочерних компонентов
            // Если родительских компонентов нет - раскрываем treeView с корня
            if (parensIDList.Count == 0)
            {
                treeView_Expanded(null, null);
            }
            // Родительские компоненты присутствуют
            else
            {
                try
                {
                    // Получение элемента TreeViewItem по данному id родителя
                    TreeViewItem treeViewItem = treeView.Items.OfType<TreeViewItem>().Where(x => (long)x.Tag == parensIDList[parentLevel]).First();
                    // Раскрытие данного узла
                    treeViewItem.IsExpanded = true;
                    // Вызов перегруженной функции, на один уровень выше
                    RefreshTreeView(treeViewItem, parentLevel + 1, parensIDList);
                }
                // Если индекс выходит за пределы массива - значит максимальный уровень пройден
                catch (InvalidOperationException)
                {
                }
            }
        }

        // Обновление элемента treeView.
        // parentLevel показывет уровень вложенности компонента в дереве. parensIDList - список Id родителей данного компонента
        private void RefreshTreeView(TreeViewItem treeViewItem, int parentLevel, List<long> parentIDList)
        {
            try
            {
                // Получение элемента TreeViewItem по данному id родителя
                TreeViewItem treeViewChildItem = treeViewItem.Items.OfType<TreeViewItem>().Where(x => (long)x.Tag == parentIDList[parentLevel]).First();
                // Раскрытие данного узла
                treeViewChildItem.IsExpanded = true;
                // Рекурсивный вызов данной функции на один уровень выше
                RefreshTreeView(treeViewChildItem, parentLevel + 1, parentIDList);
            }
            // Если индекс выходит за пределы массива - значит максимальный уровень пройден
            catch (InvalidOperationException)
            {
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        // Скрытие всех элементов treeView
        private void TreeViewCollapseAll()
        {
            // Получение корневых элементов treeView
            List<TreeViewItem> items = treeView.Items.OfType<TreeViewItem>().ToList();
            if (items.Count != 0)
            {
                // Скрытие каждого узла
                foreach (TreeViewItem tvItem in items)
                {
                    tvItem.IsExpanded = false;
                }
            }
        }

        // Создание нового объекта TreeViewItem на основе компонента
        private TreeViewItem CreateNewItemFromComponent(ComponentsAll component)
        {
            // Создание нового объекта TreeViewItem
            TreeViewItem newitem = new TreeViewItem();

            // Создание вложенного компонента
            if (component.ParentId != 0)
            {
                // Тег элемента назначается по Id текущего компонента
                newitem.Tag = componentsAllQuery.AsQueryable().Cast<ComponentsAll>()
                                                .Where(x => x.ParentId == component.ParentId)
                                                .Where(x => x.ComponentId == component.ComponentId).First().Id;

                // Наименование нового компонента  по ComponentId из списка componentsList
                string componentName = componentsUniqueQuery.AsQueryable().Cast<ComponentsUnique>()
                                                            .Where(x => x.Id == component.ComponentId).First().Name;
                // Количество компонентов
                long componentAmount = component.Amount;
                newitem.Header = componentName + " (" + componentAmount.ToString("0") + " шт.)";
            }
            // Создание нового коренного компонента
            else
            {
                // Тег элемента назначается по Id текущего компонента
                newitem.Tag = componentsAllQuery.AsQueryable().Cast<ComponentsAll>()
                                                              .Where(x => x.Name == component.Name).First().Id;

                newitem.Header = component.Name;
            }

            return newitem;
        }

        // Получение всех id уникальных дочерних элементов данного компонента из таблицы уникальных компонентов
        private List<long> GetComponentsUniqueChildrensId(ComponentsAll currentComponent)
        {
            return componentsAllQuery.AsQueryable().Cast<ComponentsAll>()
                    .Where(x => x.ParentId == currentComponent.Id).Select(s => s.ComponentId).ToList();
        }

        // Получение всех id уникальных дочерних элементов данного компонента из таблицы всех компонентов
        private void GetComponentsAllChildrensId(ComponentsAll currentComponent, ref List<long> allChildrensID)
        {
            // Если у данного компонента имеются дочерние (число дочерних компонентов != 0)
            if (componentsAllQuery.AsQueryable().Cast<ComponentsAll>()
                                                .Where(x => x.ParentId == currentComponent.Id).ToList().Count != 0)
            {
                // Цикл для всех дочерних элементов
                foreach (ComponentsAll component in componentsAllQuery.AsQueryable().Cast<ComponentsAll>()
                                                                      .Where(x => x.ParentId == currentComponent.Id))
                {
                    // Добавление данного дочернего компонента в лист
                    allChildrensID.Add(component.Id);

                    // Рекурсивный вызов данной функции для каждого дочернего компонента
                    GetComponentsAllChildrensId(component, ref allChildrensID);
                }
            }
        }

        // Получение всех id родительских элементов данного компонента из таблицы всех/уникальных компонентов
        private List<long> GetComponentsParentsIds(ComponentsAll component, ref List<long> parentIdList, bool isSearchingForUniquesComponents)
        {
            // Компонент - дочерний
            if (component.ParentId > 0)
            {
                // Получение родительского компонента по component.ParentId, т.к. родитель может быть только один, вызываем .First()
                ComponentsAll parentComponent = componentsAllQuery.AsQueryable().Cast<ComponentsAll>().Where(x => x.Id == component.ParentId).First();

                // Если выпоняется поиск в таблице уникальных компонентов
                if (isSearchingForUniquesComponents == true)
                {
                    parentIdList.Add(parentComponent.ComponentId);
                }
                // Если выпоняется поиск в таблице всех компонентов
                else
                {
                    parentIdList.Add(parentComponent.Id);
                }

                // Рекурсивный вызов данной функции для каждого родительского компонента
                return GetComponentsParentsIds(parentComponent, ref parentIdList, isSearchingForUniquesComponents);
            }

            return parentIdList;
        }

        // Получение всех id родительских элементов данного компонента из таблицы всех/уникальных компонентов
        private List<string> GetComponentsParentsNames(List<long> parentIdList)
        {
            List<string> parentNames = new List<string>();

            foreach (long id in parentIdList)
            {
                // Получение родительского компонента по component.ParentId, т.к. родитель может быть только один, вызываем .First()
                ComponentsAll parentComponent = componentsAllQuery.AsQueryable().Cast<ComponentsAll>().Where(x => x.Id == id).First();

                // Компонент - дочерний
                if (parentComponent.ParentId > 0)
                {
                    parentNames.Add(GetComponentUniquebyAllID(id).Name);
                }
                else
                {
                    parentNames.Add(parentComponent.Name);
                }
            }
            return parentNames;
        }

        // Получение компонента из таблицы всех компонентов по его ID из таблицы всех компонентов
        private ComponentsAll GetComponentAllByID(long ID)
        {
            return componentsAllQuery.AsQueryable().Cast<ComponentsAll>()
                                                   .Where(x => x.Id == ID).First();
        }

        // Получение компонента из таблицы уникальных компонентов по его ID из таблицы всех компонентов
        private ComponentsUnique GetComponentUniquebyAllID(long ID)
        {
            return componentsUniqueQuery.AsQueryable().Cast<ComponentsUnique>()
                                                      .Where(x => x.Id == GetComponentAllByID(ID).ComponentId)
                                                      .First();
        }

        // Действие при нажатии правой клавиши мыши
        private void TreeViewItem_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Получение выбранного узла дерева
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            // Обработка исключений по нажатию правой клавиши
            // Если правой клавишей был выбран объект TreeView
            try
            {
                if (treeViewItem != null)
                {
                    treeViewItem.IsSelected = true;
                    treeViewItem.ContextMenu.IsOpen = true;
                    e.Handled = true;
                }
            }
            // Если данный объект не найден (нажатие на пустом поле) показать основное контекстное меню
            catch (NullReferenceException)
            {
                treeView.ContextMenu.IsOpen = true;
            }
        }

        private static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        // Дейсивия по нажатию кнопки "Раскрыть"
        private void Button_ExpandAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in treeView.Items)
            {
                var tvi = item as TreeViewItem;
                if (tvi != null)
                    tvi.ExpandSubtree();
            }
        }
    }

    // Класс уникальных компонентов
    [Table(Name = "ComponentsList")]
    public class ComponentsUniqueClass
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public long Id { get; set; }

        [Column(Name = "Name")]
        public string Name { get; set; }
    }

    // Класс всех компонентов
    [Table(Name = "Components")]
    public class ComponentsAllClass
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public long Id { get; set; }

        [Column(Name = "ParentId")]
        public long ParentId { get; set; }

        [Column(Name = "ComponentId")]
        public long ComponentId { get; set; }

        [Column(Name = "Name")]
        public string Name { get; set; }

        [Column(Name = "Amount")]
        public long Amount { get; set; }
    }
}