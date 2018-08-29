using JqGridCodeGenerator.Commands;
using JqGridCodeGenerator.View.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Linq;

namespace JqGridCodeGenerator.ViewModel
{
    class ChooseDataBaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand PopulateComboBoxWithDatabasesCommand { get; set; }
        public ICommand HandleCredentialsFormVisibilityCommand { get; set; }
        public ICommand PopulateComboBoxWithTables { get; set; }

        private void onPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private string _SqlServerName = "Enter Sql Server Name";
        private string _Username = "Enter username";
        private string _Password = "Enter password";
        private bool _UseWindowsIdentity = false;

        public ChooseDataBaseViewModel()
        {
            PopulateComboBoxWithDatabasesCommand = new CustomCommand(Populate, CanExecuteCommand);
            HandleCredentialsFormVisibilityCommand = new CustomCommand(HandleCredentialsFormVisibility, CanExecuteCommand);
            PopulateComboBoxWithTables = new CustomCommand(PopulateWithTables, CanExecuteCommand);
        }

        private CollectionView _databaseEntries;
        private string _databaseEntry;

        public CollectionView DatabaseEntries
        {
            get { return _databaseEntries; }
            set
            {
                _databaseEntries = value;
                onPropertyChanged("DatabaseEntries");
            }
        }

        public string DatabaseEntry
        {
            get { return _databaseEntry; }
            set
            {
                if (_databaseEntry == value) return;
                _databaseEntry = value;
                onPropertyChanged("DatabaseEntry");
                PopulateComboBoxWithTables.Execute(null);
            }
        }

        private CollectionView _tables;
        private string _table;

        public CollectionView Tables
        {
            get { return _tables; }
            set
            {
                _tables = value;
                onPropertyChanged("Tables");
            }
        }

        public string Table
        {
            get { return _table; }
            set
            {
                if (_table == value) return;
                _table = value;
                onPropertyChanged("Table");
                GetColumns();
            }
        }

        public string SqlServerName
        {
            get { return _SqlServerName; }
            set
            {
                if (value != _SqlServerName)
                {
                    _SqlServerName = value;
                    onPropertyChanged("SqlServerName");
                }
            }
        }

        public string Username
        {
            get { return _Username; }
            set
            {
                if (value != _Username)
                {
                    _Username = value;
                    onPropertyChanged("Username");
                }
            }
        }

        public string Password
        {
            get { return _Password; }
            set
            {
                if (value != _Password)
                {
                    _Password = value;
                    onPropertyChanged("Password");
                }
            }
        }

        public bool UseWindowsIdentity
        {
            get { return _UseWindowsIdentity; }
            set
            {
                if (value != _UseWindowsIdentity)
                {
                    _UseWindowsIdentity = value;
                    onPropertyChanged("UseWindowsIdentity");
                    HandleCredentialsFormVisibilityCommand.Execute(null);
                }
            }
        }

        public bool CanExecuteCommand(object parameter)
        {
            return true;
        }

        public void Populate(object parameter)
        {
            List<DataBaseEntry> list = new List<DataBaseEntry>();
            var conString = CreateConnectionString();

            using (SqlConnection con = new SqlConnection(conString))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand("SELECT name from sys.databases", con))
                {
                    using (IDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new DataBaseEntry(dr[0].ToString()));
                        }
                        DatabaseEntries = new CollectionView(list);
                    }
                }
                con.Close();
            }
        }

        public void PopulateWithTables(object parameter)
        {
            List<Table> list = new List<Table>();
            var conString = CreateConnectionString();

            using (SqlConnection con = new SqlConnection(conString))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand("SELECT TABLE_NAME FROM "+DatabaseEntry+".INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", con))
                {
                    using (IDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new Table(dr[0].ToString()));
                        }
                        Tables = new CollectionView(list);
                    }
                }
                con.Close();
            }
            //GetColumns();
        }

        public void GetColumns()
        {
            //List<DataBaseEntry> list = new List<DataBaseEntry>();
            var conString = CreateConnectionString();

            using (SqlConnection conn = new SqlConnection(conString))
            {
                conn.Open();
                String[] columnRestrictions = new String[4];
                columnRestrictions[0] = DatabaseEntry;
                columnRestrictions[2] = Table;

                DataTable shemaTable = conn.GetSchema("Columns", columnRestrictions);
                
                var selectedRows = from info in shemaTable.AsEnumerable()
                    select new
                    {
                        //TableCatalog = info["TABLE_CATALOG"],
                        //TableSchema = info["TABLE_SCHEMA"],
                        //TableName = info["TABLE_NAME"],
                        ColumnName = info["COLUMN_NAME"],
                        DataType = info["DATA_TYPE"],
                        IsNullable = info["IS_NULLABLE"]
                    };

                //columnRestrictions[2] = "Course";
                columnRestrictions[3] = "PK_"+Table;

                DataTable departmentIDSchemaTable = conn.GetSchema("IndexColumns", columnRestrictions);
                var index = from info in departmentIDSchemaTable.AsEnumerable()
                                   select new
                                   {
                                       ConstraingName = info["constraint_name"],
                                       ColumnName = info["COLUMN_NAME"],
                                       IndexName = info["index_name"]
                                   };

                conn.Close();
            }
        }

        private string CreateConnectionString()
        {
            SqlConnectionStringBuilder connBuilder = new SqlConnectionStringBuilder();
            connBuilder.DataSource = SqlServerName;

            if (UseWindowsIdentity)
            {
                connBuilder.IntegratedSecurity = true;
            }
            else
            {
                connBuilder.UserID = Username;
                connBuilder.Password = Password;
            }
            if (DatabaseEntry != null)
                connBuilder.InitialCatalog = DatabaseEntry;
            return connBuilder.ConnectionString;
        }
        private void HandleCredentialsFormVisibility(object obj)
        {
            var CredentialFormStackPanel = obj as StackPanel;
            var window = JqGridCodeGeneratorWindow.Instance;
            var ChooseDataBasePage = window.PageFrame.Content as ChooseDataBasePage;

            if (_UseWindowsIdentity == true)
                ChooseDataBasePage.CredentialsForm.IsEnabled= false;
            else
                ChooseDataBasePage.CredentialsForm.IsEnabled = true;
        }
    }

    public class DataBaseEntry
    {
        public string Name { get; set; }

        public DataBaseEntry(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Table
    {
        public string Name { get; set; }

        public Table(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
