using System.Linq;
using System.Windows;

namespace ASCON_TestApp
{
    public partial class Dialog_RenameComponent : Window
    {
        private IQueryable componentsUniqueQuery;
        private IQueryable componentsAllQuery;
        public string componentNewName;
        public string componentOldName;
        private ComponentsAll currentComponent;
        private bool isNewRootComponent;

        // Конструктор класса для переименования вложенных компонентов
        public Dialog_RenameComponent(IQueryable compUniqueQuery, ComponentsAll currComponent)
        {
            InitializeComponent();

            this.Title = "Переименование компонента";
            isNewRootComponent = false;
            componentsUniqueQuery = compUniqueQuery;
            currentComponent = currComponent;
        }

        // Конструктор класса для создания новых корневых компонентов
        public Dialog_RenameComponent(IQueryable compAllQuery)
        {
            InitializeComponent();

            this.Title = "Создание нового компонента верхнего уровня";
            isNewRootComponent = true;
            componentsAllQuery = compAllQuery;
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            // Если поле ввода пустое
            if (textBox.Text == "")
            {
                string messageBoxText = "Введите новое имя компонента";
                string caption = "Введите новое имя компонента!";
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
                    // Создание компонента верхнего уровня
                    if (isNewRootComponent == true)
                    {
                        // Проверка имени на существующий компонент
                        bool alreadyInComponents = componentsAllQuery.AsQueryable().Cast<ComponentsAll>()
                                                                        .Select(x => x.Name).Any(u => u == textBox.Text);

                        // Такой компонент уже существует
                        if (alreadyInComponents == true)
                        {
                            // Вывод сообщения об ошибке
                            string messageBoxText = "Компонент с таким именем уже существует.\nВведите другое имя компонента.";
                            string caption = "Компонент с таким именем уже существует";
                            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        // Такой компонент еще не существует
                        else
                        {
                            // Присвоение имени нового компонента из textBox
                            componentNewName = textBox.Text;

                            // Вывод предуреждения о переименовании
                            string messageBoxText = "Создать новый компонент с именем \"" + componentNewName + "\"? ";
                            string caption = "Сохранение изменений";
                            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                Close();
                            }
                            if (result == MessageBoxResult.No)
                            {
                                componentNewName = null;
                                Close();
                            }
                            if (result == MessageBoxResult.Cancel)
                            {
                            }
                        }
                    }
                    // Создание вложенного компонента
                    else
                    {
                        // Проверка имени на существующий компонент
                        bool alreadyInComponents = componentsUniqueQuery.AsQueryable().Cast<ComponentsUnique>()
                                                                        .Select(x => x.Name).Any(u => u == textBox.Text);

                        // Такой компонент уже существует
                        if (alreadyInComponents == true)
                        {
                            // Вывод сообщения об ошибке
                            string messageBoxText = "Компонент с таким именем уже существует.\nВведите другое имя компонента.";
                            string caption = "Компонент с таким именем уже существует";
                            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        // Такой компонент еще не существует
                        else
                        {
                            componentNewName = textBox.Text;

                            // Вложенный компонент
                            if (currentComponent.ParentId != 0)
                            {
                                componentOldName = componentsUniqueQuery.AsQueryable().Cast<ComponentsUnique>()
                                                                          .Where(x => x.Id == currentComponent.ComponentId).First().Name;
                            }
                            // Корневой компонент
                            else
                            {
                                componentOldName = currentComponent.Name;
                            }

                            // Вывод предуреждения о переименовании
                            string messageBoxText = "Переименовать компонент \"" + componentOldName + " \" в  \"" + componentNewName + " \"? ";
                            string caption = "Сохранение изменений";
                            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                Close();
                            }
                            if (result == MessageBoxResult.No)
                            {
                                componentNewName = null;
                                Close();
                            }
                            if (result == MessageBoxResult.Cancel)
                            {
                            }
                        }
                    }
                }
            }
        }
    }
}