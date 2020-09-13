using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ASCON_TestApp
{
    public partial class MainWindow : Window
    {
        private IQueryable componentsQuery;
        private IQueryable componentsListQuery;
        private List<ComponentsListClass> componentsList;

        public MainWindow()
        {
            InitializeComponent();

            DataClasses1DataContext db = new DataClasses1DataContext();

            componentsListQuery = (from cl in db.ComponentsLists
                                   where cl is ComponentsList
                                   select cl);

            componentsList = new List<ComponentsListClass>();

            foreach (ComponentsList component in componentsListQuery)
            {
                ComponentsListClass compList = new ComponentsListClass()
                {
                    Id = component.Id,
                    Name = component.Name,
                };
                componentsList.Add(compList);
            }

            componentsQuery = (from cl in db.Components
                               where cl is Components
                               select cl);

            TreeViewFilling(componentsQuery);
        }

        private void TreeViewFilling(IQueryable components)
        {
            foreach (Components component in components)
            {
                if (component.ParentId == 0)
                {
                    TreeViewItem item = new TreeViewItem();
                    item.Tag = component.Id;
                    item.Header = component.Name.ToString();
                    item.Items.Add("*");
                    treeView.Items.Add(item);
                }
                else
                {
                }
            }
        }

        private void treeView_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)e.OriginalSource;

            int ParentId = Convert.ToInt32(item.Tag);

            item.Items.Clear();

            try
            {
                foreach (Components component in componentsQuery)
                {
                    if (component.ParentId == ParentId)
                    {
                        TreeViewItem newtem = new TreeViewItem();
                        newtem.Tag = component.Id;
                        newtem.Header = componentsList[Convert.ToInt32(component.ComponentId)].Name;
                        newtem.Items.Add("*");
                        item.Items.Add(newtem);
                    }
                }
            }
            catch
            { }
        }
    }

    [Table(Name = "ComponentsList")]
    public class ComponentsListClass
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public long Id { get; set; }

        [Column(Name = "Name")]
        public string Name { get; set; }
    }

    [Table(Name = "Components")]
    public class ComponentsClass
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