using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Word = Microsoft.Office.Interop.Word;

namespace ASCON_TestApp
{
    internal class ExportToWord
    {
        private Word.Application wordapp;
        private Word.Document worddocument;
        private Word.Paragraph wordparagraph;
        private string fileName;

        private IQueryable componentsAllQuery;
        private IQueryable componentsUniqueQuery;
        private ComponentsAll currentComponent;

        internal ExportToWord(ComponentsAll curComponent, IQueryable compsAllQuery, IQueryable compsUniqueQuery, List<long> allChildrensID)
        {
            componentsAllQuery = compsAllQuery;
            componentsUniqueQuery = compsUniqueQuery;
            currentComponent = curComponent;

            // Нахождение имени компонента из БД
            string componentName;

            // Для корневых компонентов
            if (currentComponent.ComponentId == 0)
            {
                componentName = currentComponent.Name;
            }
            // Для дочерних компонентов
            else
            {
                componentName = componentsUniqueQuery.AsQueryable().Cast<ComponentsUnique>()
                                                                    .Where(x => x.Id == currentComponent.ComponentId)
                                                                    .First().Name;
            }

            // Инициализация сортированного словаря, где ключом является имя компонента, а значением - суммарное количество включений в данный компонент
            SortedDictionary<string, long> uniqueCompNamesWithTotalAmount = new SortedDictionary<string, long>();

            // Получение значений через лист allChildrensID - id дочерних компонентов данного компонента
            uniqueCompNamesWithTotalAmount = GetTotalAmountOfComponent(allChildrensID);

            // Количество строк таблицы
            int rowNumber = uniqueCompNamesWithTotalAmount.Count;

            //--------------------------------------

            // Создание экземпляра приложения MS Word
            wordapp = new Word.Application();

            // Установление видимости
            wordapp.Visible = true;

            // Свойства нового документа
            Object template = Type.Missing;
            Object newTemplate = false;
            Object documentType = Word.WdNewDocumentType.wdNewBlankDocument;
            Object visible = true;
            fileName = Directory.GetCurrentDirectory() + @"\Отчет о сводном составе - " + componentName + ".docx";

            //Создание нового документа
            worddocument = wordapp.Documents.Add(ref template, ref newTemplate, ref documentType, ref visible);

            //--------------------------------------

            // Свойства новых параграфов
            object oMissing = System.Reflection.Missing.Value;

            // Создание двух параграфов
            worddocument.Paragraphs.Add(ref oMissing);
            worddocument.Paragraphs.Add(ref oMissing);

            // Индекс текущей строки
            int currentRow = 1;

            // Получение параграфов текущей строки
            wordparagraph = worddocument.Paragraphs[currentRow];

            //Ввод текста в первый параграф
            wordparagraph.Range.Text = "Отчет о сводном составе для компонента \"" + componentName + "\":";

            // Переход на следующую строку
            currentRow++;

            //--------------------------------------

            // Получение параграфов текущей строки
            wordparagraph = worddocument.Paragraphs[currentRow];

            //Добавление таблицы в документ
            Word.Range wordrange = wordparagraph.Range;
            object defaultTableBehavior = Word.WdDefaultTableBehavior.wdWord9TableBehavior;
            object autoFitBehavior = Word.WdAutoFitBehavior.wdAutoFitWindow;

            //Добавляем таблицу и получаем объект wordtable
            Word.Table wordtable = worddocument.Tables.Add(wordrange, rowNumber, 2, ref defaultTableBehavior, ref autoFitBehavior);

            // Заполнение таблицы
            for (int i = 1; i <= rowNumber; i++)
            {
                // Добавление значений в выбранную ячейку первого столбца
                Word.Range wordcellrange1Column = wordtable.Cell(i, 1).Range;
                wordcellrange1Column.Text = uniqueCompNamesWithTotalAmount.ElementAt(i - 1).Key;

                // Добавление значений в выбранную ячейку второго столбца
                Word.Range wordcellrange2Column = wordtable.Cell(i, 2).Range;
                wordcellrange2Column.Text = uniqueCompNamesWithTotalAmount.ElementAt(i - 1).Value.ToString() + " шт.";
            }

            //--------------------------------------

            // Сохранение с именем fileName
            try
            {
                worddocument.SaveAs2(fileName);
            }
            // Если файл уже открыт
            catch (Exception e)
            {
                string messageBoxText = e.Message;
                string caption = "Ошибка";
                MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Получение значений через лист allChildrensID - id дочерних компонентов данного компонента
        private SortedDictionary<string, long> GetTotalAmountOfComponent(List<long> allChildrensID)
        {
            SortedDictionary<string, long> uniqueCompNamesWithTotalAmount = new SortedDictionary<string, long>();

            // Цикл для всех дочерних компонентов
            foreach (long allId in allChildrensID)
            {
                // уникальный компонент, находится по ID компонента из общей таблицы
                ComponentsUnique componentUnique = GetComponentUniquebyAllID(allId);

                // Словарь еще не содержит такого компонента
                if (!uniqueCompNamesWithTotalAmount.ContainsKey(componentUnique.Name))
                {
                    // Нахождение общего количества компонентов
                    long amount = GetComponentAmountByID(componentUnique.Id, allChildrensID);

                    // Условие, позволяющее не включать в конечный протокол промежуточные компоненты, например "Поршень в сборе"
                    if (IsHavingChildrens(allId) == false)
                    {
                        // Добавление в словарь
                        uniqueCompNamesWithTotalAmount.Add(componentUnique.Name, amount);
                    }
                }
            }

            return uniqueCompNamesWithTotalAmount;
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

        // Получение общего количества включений данного уникального компонента по его id
        private long GetComponentAmountByID(long uniqueComponentID, List<long> allChildrensID)
        {
            long amount = 0;

            // Цикл для всех дочерних компонентов
            foreach (long id in allChildrensID)
            {
                // Получение компонента из таблицы всех компонентов по его ID из таблицы всех компонентов
                ComponentsAll component = GetComponentAllByID(id);

                // Если этот компонент является искомым уникальным компонентом
                if (component.ComponentId == uniqueComponentID)
                {
                    // Если родитель данного уникального компонента является компонентом, для которого формируется отчет
                    if (GetComponentAllByID(component.ParentId).Id == currentComponent.Id)
                    {
                        // Добавление количества данного компонента к общей сумме
                        amount += component.Amount;
                    }
                    // Случай, если у данного компонента родитель не является  компонентом, для которого формируется отчет
                    else
                    {
                        // Добавление количества данного компонента к общей сумме
                        amount += component.Amount;

                        // Рекурсивный вызов перегруженной  функции для просмотра вложенных компонентов
                        amount = GetComponentAmountByID(component, amount);
                    }
                }
            }

            return amount;
        }

        // Получение общего количества включений данного уникального компонента
        private long GetComponentAmountByID(ComponentsAll component, long amount)
        {
            try
            {               
                // Получение родительского компонента из таблицы всех компонентов по его ID из таблицы всех компонентов
                ComponentsAll parentComponent = GetComponentAllByID(component.ParentId);

                // Выход из рекурсии, если достигнут целевой родитель
                if (parentComponent.Id == currentComponent.Id)
                {
                    return 1;
                }
                else
                {
                    // Перемножение количества дочерних компонентов на количество родительских компонентов
                    amount *= parentComponent.Amount;
                    // Рекурсивный вызов перегруженной  функции для просмотра вложенных компонентов
                    amount *= GetComponentAmountByID(parentComponent, amount);
                }
            }
            // Выход из рекурсии
            catch (Exception)
            {
            }

            return amount;
        }

        // Функция, показывающая, имеет ли данный компонент дочерние компоненты - по его id из таблицы всех компонентов
        private bool IsHavingChildrens(long allId)
        {
            if (componentsAllQuery.AsQueryable().Cast<ComponentsAll>().Where(x => x.ParentId == allId).ToList().Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}