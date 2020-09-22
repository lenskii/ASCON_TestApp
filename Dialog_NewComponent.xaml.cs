using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ASCON_TestApp
{
    /// <summary>
    /// Interaction logic for Dialog_NewComponent.xaml
    /// </summary>
    public partial class Dialog_NewComponent : Window
    {
        public ComponentsUnique newComponent;
        private IQueryable componentsUniqueQuery;
        public int newComponentAmount;

        public Dialog_NewComponent(IQueryable compUniqueQuery, List<long> idList)
        {
            InitializeComponent();

            componentsUniqueQuery = compUniqueQuery;

            // Добавление в comboBox первого элемента - при его выборе можно задать имя нового компонента
            comboBox.Items.Add("Новый компонент...");
            textBox.IsEnabled = true;

            // Заполнение comboBox уникальными компонентами
            foreach (ComponentsUnique component in componentsUniqueQuery)
            {
                bool alreadyInList = idList.Any(u => u == component.Id);
                if (alreadyInList == false)
                {
                    comboBox.Items.Add(component.Name);
                }
            }
            comboBox.SelectedIndex = 0;
        }

        // Включение или выключение поля ввода в зависимости от выбранного пункта comboBox
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox.SelectedIndex == 0)
            {
                textBox.IsEnabled = true;
            }
            else
            {
                textBox.IsEnabled = false;
            }
        }

        // Действие по нажатию кнопки ОК
        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            // Создание нового компонента
            newComponent = new ComponentsUnique();

            // Создание нового уникального компонента через поле ввода
            if (comboBox.SelectedIndex == 0)
            {
                // Если поле ввода пустое
                if (textBox.Text == "")
                {
                    string messageBoxText = "Введите имя нового компонента";
                    string caption = "Введите имя нового компонента!";
                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                // Если поле ввода не пустое
                else
                {
                    if (textBox.Text.Length > 50)
                    {
                        string messageBoxText = "Имя компонента слишком длинное.\nМаксимальная длина имени компонента - 50 символов";
                        string caption = "Имя компонента слишком длинное";
                        MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);
                        textBox.Text = "";
                    }
                    else
                    {
                        // Проверка имени на существующий компонент
                        bool alreadyInList = componentsUniqueQuery.AsQueryable().Cast<ComponentsUnique>().Select(x => x.Name).Any(u => u == textBox.Text);
                        if (alreadyInList)
                        {
                            // Вывод предуреждения
                            string messageBoxText = "Компонент \"" + textBox.Text + "\" уже присутствует в базе данных. Использовать его?";
                            string caption = "Данный компонент уже присутствует в базе данных";
                            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.OKCancel, MessageBoxImage.Question);

                            // Создание компонента из списка существующих по введенному имени
                            if (result == MessageBoxResult.OK)
                            {
                                newComponent = componentsUniqueQuery.AsQueryable().Cast<ComponentsUnique>()
                                                       .Where(x => x.Name == textBox.Text).First();
                            }
                            if (result == MessageBoxResult.Cancel)
                            {
                            }
                        }
                        // Такой компонент еще не существует
                        else
                        {
                            // Индекс компонента должен быть больше, чем все предыдущие, во избежание перезаписи таблицы
                            long currentMaxID = componentsUniqueQuery.AsQueryable().Cast<ComponentsUnique>().Max(t => t.Id);
                            newComponent.Id = currentMaxID + 1;

                            // Присвоение имени нового компонента из textBox
                            newComponent.Name = textBox.Text;
                        }
                    }

                    
                }
            }
            // Выбор уже существующего компонента
            else
            {
                // Создание компонента из списка существующих по введенному имени
                newComponent = componentsUniqueQuery.AsQueryable().Cast<ComponentsUnique>()
                                                    .Where(x => x.Name == comboBox.SelectedItem.ToString()).First();
            }

            // Если новый компонент существует 
            if (newComponent.Name != null)
            {
                // Парсим количество новых компонентов
                try
                {
                    newComponentAmount = Convert.ToInt32(amountTextBox.Text);
                    Close();
                }
                catch (FormatException)
                {
                    string messageBoxText = "Введите корректное количество новых компонентов";
                    string caption = "Введите корректное количество новых компонентов!";
                    MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}