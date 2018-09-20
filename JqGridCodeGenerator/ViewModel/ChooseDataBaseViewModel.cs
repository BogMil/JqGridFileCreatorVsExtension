//DUSANRAZVOJ-PC\SQLEXPRESS
using JqGridCodeGenerator.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using EnvDTE;
using JqGridCodeGenerator.T4Templates;

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using System.Windows;
using Microsoft.VisualStudio.TextTemplating;

namespace JqGridCodeGenerator.ViewModel
{
    class ChooseDataBaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand PopulateComboBoxWithDatabasesCommand { get; set; }
        public ICommand HandleCredentialsFormVisibilityCommand { get; set; }
        public ICommand HandleControllersComboBoxVisibilityCommand { get; set; }
        public ICommand HandleRepositoriesComboBoxVisibilityCommand { get; set; }
        public ICommand PopulateComboBoxWithTables { get; set; }
        public ICommand CreateFilesCommand { get; set; }

        public List<Column> Columns = new List<Column>();

        private void onPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public List<string> AddedFolders;

        public ChooseDataBaseViewModel()
        {
            PopulateComboBoxWithDatabasesCommand = new CustomCommand(Populate, CanExecuteCommand);
            HandleCredentialsFormVisibilityCommand = new CustomCommand(HandleCredentialsFormVisibility, CanExecuteCommand);
            HandleControllersComboBoxVisibilityCommand = new CustomCommand(HandleControllersComboBoxVisibility, CanExecuteCommand);
            HandleRepositoriesComboBoxVisibilityCommand = new CustomCommand(HandleRepositoriesComboBoxVisibility, CanExecuteCommand);
            PopulateComboBoxWithTables = new CustomCommand(PopulateWithTables, CanExecuteCommand);
            CreateFilesCommand = new CustomCommand(CreateFiles, CanExecuteCommand);

            AddedFolders = new List<string>();
            GetControllers();
            GetServices(); 
            GetRepositories();

            string message = "Kreirani su sledeci folderi koji su bitni za rad GenericCSR framework-a:\n-----------------------\n";
            foreach (var addedFolder in AddedFolders)
                message += addedFolder+"\n-----------------------\n";

            if(AddedFolders.Count>0)
                MessageBox.Show(message,"Informacija");

            IsUseBaseControllerEnabled = BaseControllers.Count>0;
            IsUseBaseRepositoryEnabled = BaseRepositories.Count>0;
        }

        private Column _primaryKeyColumn;
        private List<TypeWithNamespace> _controllersWithNamespaces = null;
        private List<TypeWithNamespace> _repositoriesWithNamespaces = null;
        private string _baseControllerNamespace = null;
        private string _baseRepositoryNamespace = null;

        private CollectionView _databaseEntries;
        public CollectionView DatabaseEntries
        {
            get { return _databaseEntries; }
            set
            {
                _databaseEntries = value;
                onPropertyChanged("DatabaseEntries");
            }
        }

        private string _databaseEntry;
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
        public CollectionView Tables
        {
            get { return _tables; }
            set
            {
                _tables = value;
                onPropertyChanged("Tables");
            }
        }

        private string _table;
        public string Table
        {
            get { return _table; }
            set
            {
                if (_table == value) return;
                _table = value;
                GetColumns();
                onPropertyChanged("Table");
            }
        }

        private CollectionView _baseControllers;
        public CollectionView BaseControllers
        {
            get { return _baseControllers; }
            set
            {
                _baseControllers = value;
                onPropertyChanged("BaseControllers");
            }
        }

        private string _baseController;
        public string BaseController
        {
            get { return _baseController; }
            set
            {
                if (_baseController == value) return;
                _baseController = value;
                _baseControllerNamespace = _controllersWithNamespaces.GetNamespaceForType(value);
                onPropertyChanged("BaseController");
            }
        }

        private CollectionView _baseRepositories;
        public CollectionView BaseRepositories
        {
            get { return _baseRepositories; }
            set
            {
                _baseRepositories = value;
                onPropertyChanged("BaseRepositories");
            }
        }

        private string _baseRepository;
        public string BaseRepository
        {
            get { return _baseRepository; }
            set
            {
                if (_baseRepository == value) return;
                _baseRepository = value;
                _baseRepositoryNamespace = _repositoriesWithNamespaces.GetNamespaceForType(value);
                onPropertyChanged("BaseRepository");
            }
        }

        private string _SqlServerName = "Enter Sql Server Name";
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

        private string _Username = "Enter username";
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

        private string _Password = "Enter password";
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

        private string _baseName="BaseName";
        public string BaseName
        {
            get { return _baseName; }
            set
            {
                if (value != _baseName)
                {
                    _baseName = value;
                    onPropertyChanged("BaseName");
                }
            }
        }

        private bool _UseWindowsIdentity = false;
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

        private bool _UseBaseController = false;
        public bool UseBaseController
        {
            get { return _UseBaseController; }
            set
            {
                if (value != _UseBaseController)
                {
                    _UseBaseController = value;
                    onPropertyChanged("UseBaseController");
                    HandleControllersComboBoxVisibilityCommand.Execute(null);
                }
            }
        }

        private bool _IsUseBaseControllerEnabled = false;
        public bool IsUseBaseControllerEnabled
        {
            get { return _IsUseBaseControllerEnabled; }
            set
            {
                if (value != _IsUseBaseControllerEnabled)
                {
                    _IsUseBaseControllerEnabled = value;
                    onPropertyChanged("IsUseBaseControllerEnabled");
                }
            }
        }

        private bool _IsUseBaseRepositoryEnabled = false;
        public bool IsUseBaseRepositoryEnabled
        {
            get { return _IsUseBaseRepositoryEnabled; }
            set
            {
                if (value != _IsUseBaseRepositoryEnabled)
                {
                    _IsUseBaseRepositoryEnabled = value;
                    onPropertyChanged("IsUseBaseRepositoryEnabled");
                }
            }
        }

        private bool _UseBaseRepository = false;
        public bool UseBaseRepository
        {
            get { return _UseBaseRepository; }
            set
            {
                if (value != _UseBaseRepository)
                {
                    _UseBaseRepository = value;
                    onPropertyChanged("UseBaseService");
                    HandleRepositoriesComboBoxVisibilityCommand.Execute(null);
                }
            }
        }

        public bool CanExecuteCommand(object parameter)
        {
            return true;
        }

        public void Populate(object parameter)
        {
            List<ComboBoxItem> list = new List<ComboBoxItem>();

            using (SqlConnection con = new SqlConnection(CreateConnectionString()))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand("SELECT name from sys.databases", con))
                {
                    using (IDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new ComboBoxItem(dr[0].ToString()));
                        }
                        DatabaseEntries = new CollectionView(list);
                    }
                }
                con.Close();
            }
        }

        public void PopulateWithTables(object parameter)
        {
            List<ComboBoxItem> list = new List<ComboBoxItem>();
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
                            list.Add(new ComboBoxItem(dr[0].ToString()));
                        }
                        Tables = new CollectionView(list);
                    }
                }
                con.Close();
            }
        }

        public void GetColumns()
        {
            List<ComboBoxItem> list = new List<ComboBoxItem>();
            using (SqlConnection conn = new SqlConnection(CreateConnectionString()))
            {
                conn.Open();
                String[] columnRestrictions = new String[4];
                columnRestrictions[0] = DatabaseEntry;
                columnRestrictions[2] = Table;

                DataTable shemaTable = conn.GetSchema("Columns", columnRestrictions);

                var columns = (from info in shemaTable.AsEnumerable()
                    select new Column()
                    {
                        Name = info["COLUMN_NAME"].ToString(),
                        Type = info["DATA_TYPE"].ToString(),
                        IsNullable = info["IS_NULLABLE"].ToString() == "YES"
                    }).ToList();

                Columns = new List<Column>();
                foreach (var column in columns)
                    Columns.Add(column);

                columnRestrictions[3] = "PK_"+Table;

                shemaTable = conn.GetSchema("IndexColumns", columnRestrictions);
                var primaryKey = (from info in shemaTable.AsEnumerable()
                    select new{
                        ColumnName = info["COLUMN_NAME"].ToString()
                    }).ToList().FirstOrDefault();

                if (primaryKey != null)
                {
                    foreach (var column in columns)
                        if (column.Name == primaryKey.ColumnName)
                            column.IsPrimaryKey = true;
                        
                    _primaryKeyColumn = columns.GetPrimaryKeyColumn();
                }
                else
                {
                    _primaryKeyColumn = null;
                    MessageBox.Show("Tabela nema primarni kljuc. Morate izabrati tabelu koja ima primarni kljuc", "Informacija");
                }

                conn.Close();
            }
        }

        private string CreateConnectionString()
        {
            SqlConnectionStringBuilder connBuilder = new SqlConnectionStringBuilder
            {
                DataSource = SqlServerName
            };

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

            if (UseWindowsIdentity == true)
                CredentialFormStackPanel.IsEnabled= false;
            else
                CredentialFormStackPanel.IsEnabled = true;
        }

        private void HandleControllersComboBoxVisibility(object obj)
        {
            var ControllersComboBox = obj as ComboBox;

            if (UseBaseController != true)
                ControllersComboBox.IsEnabled = false;
            else
                ControllersComboBox.IsEnabled = true;
        }

        private void HandleRepositoriesComboBoxVisibility(object obj)
        {
            var ServicesComboBox = obj as ComboBox;

            if (UseBaseRepository != true)
                ServicesComboBox.IsEnabled = false;
            else
                ServicesComboBox.IsEnabled = true;
        }

        public void CreateFiles(object parametar)
        {
            if (Table == String.Empty || Table == null)
            {
                MessageBox.Show("Изабери табелу", "Informacija");
                return;
            }

            if (_primaryKeyColumn == null)
            {
                MessageBox.Show("Tabela nema primarni kljuc. Nije moguce koristiti GenericCRS za tabele koje nemaju primarni kljuc", "Informacija");
                return;
            }

            var rootFolder = GetRootFolder();

            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            var activeProject = GetActiveProject(dte);

            CreateIServiceFile(activeProject,rootFolder);
            CreateServiceFile(activeProject, rootFolder);
            CreateIRepositoryFile(activeProject, rootFolder);
            CreateRepositoryFile(activeProject, rootFolder);
            CreateControllerFile(activeProject, rootFolder);
            CreateModelFile(activeProject, rootFolder);

            JqGridCodeGeneratorWindow.Instance.Close();
        }

        private void CreateIServiceFile(Project activeProject,DirectoryInfo rootFolder)
        {
            var filePath = rootFolder.FullName + "\\Services\\CRUD\\Interfaces\\I"+BaseName+"Service.cs";
            var file = File.Create(filePath);
            file.Close();
            IServiceTemplate page = new IServiceTemplate();
            page.Session = new TextTemplatingSession
            {
                ["baseName"] = BaseName,
                ["baseNamespace"] = GetBaseNamespace(rootFolder)
            };
            page.Initialize();

            var pageContent = page.TransformText();
            File.WriteAllText(filePath, pageContent);

            activeProject.ProjectItems.AddFromFile(filePath);

            ItemOperations ItemOp = activeProject.DTE.ItemOperations;
            ItemOp.OpenFile(filePath, EnvDTE.Constants.vsViewKindTextView);
        }

        private void CreateIRepositoryFile(Project activeProject, DirectoryInfo rootFolder)
        {
            var filePath = rootFolder.FullName + "\\Repositories\\Interfaces\\I" + BaseName + "Repository.cs";
            var file = File.Create(filePath);
            file.Close();
            IRepositoryTemplate page = new IRepositoryTemplate();
            page.Session = new TextTemplatingSession
            {
                ["baseName"] = BaseName,
                ["tableName"] = Table,
                ["baseNamespace"] = GetBaseNamespace(rootFolder)
            };
            page.Initialize();

            var pageContent = page.TransformText();
            File.WriteAllText(filePath, pageContent);

            activeProject.ProjectItems.AddFromFile(filePath);

            ItemOperations ItemOp = activeProject.DTE.ItemOperations;
            ItemOp.OpenFile(filePath, EnvDTE.Constants.vsViewKindTextView);
        }

        private void CreateRepositoryFile(Project activeProject, DirectoryInfo rootFolder)
        {
            var filePath = rootFolder.FullName + "\\Repositories\\" + BaseName + "Repository.cs";
            var file = File.Create(filePath);
            file.Close();
            RepositoryTemplate page = new RepositoryTemplate();
            page.Session = new TextTemplatingSession
            {
                ["baseName"] = BaseName,
                ["tableName"] = Table,
                ["baseNamespace"] = GetBaseNamespace(rootFolder),
                ["primaryKeyName"] = _primaryKeyColumn.Name,
                ["useBaseRepository"] = _UseBaseRepository,
                ["baseRepositoryNamespace"] = _baseRepositoryNamespace,
                ["baseRepositoryName"] = BaseRepository
            };
            page.Initialize();

            var pageContent = page.TransformText();
            File.WriteAllText(filePath, pageContent);

            activeProject.ProjectItems.AddFromFile(filePath);

            ItemOperations ItemOp = activeProject.DTE.ItemOperations;
            ItemOp.OpenFile(filePath, EnvDTE.Constants.vsViewKindTextView);
        }

        private void CreateControllerFile(Project activeProject, DirectoryInfo rootFolder)
        {
            var filePath = rootFolder.FullName + "\\Controllers\\"+ BaseName + "Controller.cs";
            var file = File.Create(filePath);
            file.Close();
            ControllerTemplate page = new ControllerTemplate();
            page.Session = new TextTemplatingSession
            {
                ["baseName"] = BaseName,
                ["baseNamespace"] = GetBaseNamespace(rootFolder),
                ["useBaseController"] = _UseBaseController,
                ["baseControllerNamespace"] = _baseControllerNamespace,
                ["baseControllerName"] = BaseController
            };
            page.Initialize();

            var pageContent = page.TransformText();
            File.WriteAllText(filePath, pageContent);

            activeProject.ProjectItems.AddFromFile(filePath);

            ItemOperations ItemOp = activeProject.DTE.ItemOperations;
            ItemOp.OpenFile(filePath, EnvDTE.Constants.vsViewKindTextView);
        }

        private void CreateModelFile(Project activeProject, DirectoryInfo rootFolder)
        {
            var filePath = rootFolder.FullName + "\\Models\\" + BaseName + ".cs";
            var file = File.Create(filePath);
            file.Close();
            ModelTemplate page = new ModelTemplate();
            page.Session = new TextTemplatingSession
            {
                ["baseName"] = BaseName,
                ["baseNamespace"] = GetBaseNamespace(rootFolder),
                ["tableName"] = Table,
                ["primaryKeyColumn"] = _primaryKeyColumn,
                ["columns"] = Columns
            };
            page.Initialize();

            var pageContent = page.TransformText();
            File.WriteAllText(filePath, pageContent);

            activeProject.ProjectItems.AddFromFile(filePath);

            ItemOperations ItemOp = activeProject.DTE.ItemOperations;
            ItemOp.OpenFile(filePath, EnvDTE.Constants.vsViewKindTextView);
        }

        private void CreateServiceFile(Project activeProject, DirectoryInfo rootFolder)
        {
            var filePath = rootFolder.FullName + "\\Services\\CRUD\\" + BaseName + "Service.cs";
            var file = File.Create(filePath);
            file.Close();
            ServiceTemplate page = new ServiceTemplate();
            page.Session = new TextTemplatingSession
            {
                ["baseName"] = BaseName,
                ["baseNamespace"] = GetBaseNamespace(rootFolder),
                ["tableName"] = Table
            };
            page.Initialize();

            var pageContent = page.TransformText();
            File.WriteAllText(filePath, pageContent);

            activeProject.ProjectItems.AddFromFile(filePath);

            ItemOperations ItemOp = activeProject.DTE.ItemOperations;
            ItemOp.OpenFile(filePath, EnvDTE.Constants.vsViewKindTextView);
        }

        private string GetBaseNamespace(DirectoryInfo rootFolder)
        {
            if (IsRootFolderInArea(rootFolder))
                return rootFolder.Parent.Parent.Name + "." + rootFolder.Parent.Name + "." + rootFolder.Name;
            return rootFolder.Name;
        }

        public static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
        {
            hierarchy = null;
            itemid = VSConstants.VSITEMID_NIL;
            int hr = VSConstants.S_OK;

            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return false;
            }

            IVsMultiItemSelect multiItemSelect = null;
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainerPtr = IntPtr.Zero;

            try
            {
                hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);

                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    // there is no selection
                    return false;
                }

                // multiple items are selected
                if (multiItemSelect != null) return false;

                // there is a hierarchy root node selected, thus it is not a single item inside a project

                if (itemid == VSConstants.VSITEMID_ROOT) return false;

                hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null) return false;

                Guid guidProjectID = Guid.Empty;

                if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectID)))
                {
                    return false; // hierarchy is not a project inside the Solution if it does not have a ProjectID Guid
                }

                // if we got this far then there is a single project item selected
                return true;
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPtr);
                }

                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }

        public Project GetActiveProject(DTE dte)
        {
            var activeProjects = dte.ActiveSolutionProjects as Array;
            return (Project)activeProjects.GetValue(0);
        }

        public void GetControllers()
        {
            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            var activeProject = GetActiveProject(dte);
            IVsSolution solution = (IVsSolution)Package.GetGlobalService(typeof(IVsSolution));

            var rootFolder = GetRootFolder();

            List<TypeWithNamespace> listOfTypesWithNamespaces = new List<TypeWithNamespace>();
            var controllerFolder = activeProject.GetProjectItemByName("Controllers");
            foreach (ProjectItem controller in controllerFolder.ProjectItems)
            {
                listOfTypesWithNamespaces= PopulateListWithTypesThatInheritBaseType(controller,listOfTypesWithNamespaces/*, "GenericController"*/);
            }

            if (IsRootFolderInArea(rootFolder))
            {
                var areasFolder = activeProject.GetProjectItemByName("Areas");
                var areasRootFolder = areasFolder.GetProjectItemByName(rootFolder.Name);
                var areasControllerFolder = areasRootFolder.GetProjectItemByName("Controllers");
                foreach (ProjectItem controller in areasControllerFolder.ProjectItems)
                {
                    listOfTypesWithNamespaces = PopulateListWithTypesThatInheritBaseType(controller, listOfTypesWithNamespaces/*,"GenericController"*/);
                }
            }

            var listOfControllerName = listOfTypesWithNamespaces.Select(x => x.Name).ToList();
            var comboBoxList = new List<ComboBoxItem>();

            foreach(var controllerName in listOfControllerName)
            {
                comboBoxList.Add(new ComboBoxItem(controllerName));
            }
            _controllersWithNamespaces = listOfTypesWithNamespaces;
            BaseControllers = new CollectionView(comboBoxList);
        }

        public void GetServices()
        {
            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            var activeProject = GetActiveProject(dte);
            IVsSolution solution = (IVsSolution)Package.GetGlobalService(typeof(IVsSolution));

            var rootFolder = GetRootFolder();

            List<ComboBoxItem> list = new List<ComboBoxItem>();
            var servicesFolder = activeProject.GetProjectItemByName("Services");
            
            if (servicesFolder == null)
            {
                servicesFolder=activeProject.ProjectItems.AddFolder("Services");
                var crudFolder = servicesFolder.ProjectItems.AddFolder("CRUD");
                crudFolder.ProjectItems.AddFolder("Interfaces");

                AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Services");
                AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Services\\CRUD");
                AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Services\\CRUD\\Interfaces");
            }
            else
            {
                var crudFolder = servicesFolder.GetProjectItemByName("CRUD");
                if (crudFolder==null)
                {
                    crudFolder=servicesFolder.ProjectItems.AddFolder("CRUD");
                    crudFolder.ProjectItems.AddFolder("Interfaces");

                    AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Services\\CRUD");
                    AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Services\\CRUD\\Interfaces");
                }
                var interfacesFolder = crudFolder.GetProjectItemByName("Interfaces");
                if (interfacesFolder == null)
                {
                    crudFolder.ProjectItems.AddFolder("Interfaces");
                    AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Services\\CRUD\\Interfaces");
                }
            }

            if (IsRootFolderInArea(rootFolder))
            {
                var areasFolder = activeProject.GetProjectItemByName("Areas");
                var rootFolderInAreas = areasFolder.GetProjectItemByName(rootFolder.Name);
                var ServicesFolderInRoot = rootFolderInAreas.GetProjectItemByName("Services");
                if (ServicesFolderInRoot == null)
                {
                    ServicesFolderInRoot=rootFolderInAreas.ProjectItems.AddFolder("Services");
                    var crudFolder = ServicesFolderInRoot.ProjectItems.AddFolder("CRUD");
                    crudFolder.ProjectItems.AddFolder("Interfaces");

                    AddedFolders.Add(rootFolder.Parent.Parent.Name+"\\"+rootFolder.Parent.Name+"\\"+rootFolder.Name + "\\Services");
                    AddedFolders.Add(rootFolder.Parent.Parent.Name+"\\"+rootFolder.Parent.Name+"\\"+rootFolder.Name + "\\Services\\CRUD");
                    AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" + rootFolder.Name + "\\Services\\CRUD\\Interfaces");
                }
                else
                {
                    var crudFolder = ServicesFolderInRoot.GetProjectItemByName("CRUD");
                    if (crudFolder == null)
                    {
                        crudFolder = ServicesFolderInRoot.ProjectItems.AddFolder("CRUD");
                        crudFolder.ProjectItems.AddFolder("Interfaces");

                        AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" + rootFolder.Name + "\\Services\\CRUD");
                        AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" + rootFolder.Name + "\\Services\\CRUD\\Interfaces");
                    }
                    var interfacesFolder = crudFolder.GetProjectItemByName("Interfaces");
                    if (interfacesFolder == null)
                    {
                        crudFolder.ProjectItems.AddFolder("Interfaces");

                        AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" + rootFolder.Name + "\\Services\\CRUD\\Interfaces");
                    }
                }
            }
        }

        public void GetRepositories()
        {
            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            var activeProject = GetActiveProject(dte);
            IVsSolution solution = (IVsSolution)Package.GetGlobalService(typeof(IVsSolution));

            var rootFolder = GetRootFolder();

            List<TypeWithNamespace> listOfTypesWithNamespaces = new List<TypeWithNamespace>();
            var repositoriesFolder = activeProject.GetProjectItemByName("Repositories");

            if (repositoriesFolder == null)
            {
                repositoriesFolder=activeProject.ProjectItems.AddFolder("Repositories");
                repositoriesFolder.ProjectItems.AddFolder("Interfaces");
                AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Repositories");
                AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Repositories\\Interfaces");
            }
            else
            {
                var interfacesFolder = repositoriesFolder.GetProjectItemByName("Interfaces");
                if (interfacesFolder == null)
                {
                    repositoriesFolder.ProjectItems.AddFolder("Interfaces");
                    AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Repositories\\Interfaces");
                }
               
                foreach (ProjectItem repository in repositoriesFolder.ProjectItems)
                {
                    listOfTypesWithNamespaces = PopulateListWithTypesThatInheritBaseType(repository, listOfTypesWithNamespaces/*, "GenericRepository"*/);
                }
               
            }

            if (IsRootFolderInArea(rootFolder))
            {
                var areasFolder = activeProject.GetProjectItemByName("Areas");
                var rootFolderInAreas = areasFolder.GetProjectItemByName(rootFolder.Name);
                var RepositoriesFolderInRoot = rootFolderInAreas.GetProjectItemByName("Repositories");
                if (RepositoriesFolderInRoot == null)
                {
                    RepositoriesFolderInRoot= rootFolderInAreas.ProjectItems.AddFolder("Repositories");
                    RepositoriesFolderInRoot.ProjectItems.AddFolder("Interfaces");

                    AddedFolders.Add(rootFolder.Parent.Parent.Name+"\\"+rootFolder.Parent.Name+"\\"+rootFolder.Name + "\\Repositories");
                    AddedFolders.Add(rootFolder.Parent.Parent.Name +"\\"+ rootFolder.Parent.Name + "\\" + rootFolder.Name+"\\Repositories\\Interfaces");
                }
                else
                {
                    var interfacesFolder = RepositoriesFolderInRoot.GetProjectItemByName("Interfaces");
                    if (interfacesFolder == null)
                    {
                        RepositoriesFolderInRoot.ProjectItems.AddFolder("Interfaces");
                        AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" + rootFolder.Name + "\\Repositories\\Interfaces");
                    }
                   
                    foreach (ProjectItem repository in RepositoriesFolderInRoot.ProjectItems)
                    {
                        listOfTypesWithNamespaces = PopulateListWithTypesThatInheritBaseType(repository, listOfTypesWithNamespaces/*, "GenericRepository"*/);
                    }
                }
            }

            var listOfRepositoryName = listOfTypesWithNamespaces.Select(x => x.Name).ToList();
            var comboBoxList = new List<ComboBoxItem>();

            foreach (var repositoryName in listOfRepositoryName)
            {
                comboBoxList.Add(new ComboBoxItem(repositoryName));
            }
            _repositoriesWithNamespaces = listOfTypesWithNamespaces;
            BaseRepositories = new CollectionView(comboBoxList);
        }


        public List<TypeWithNamespace> PopulateListWithTypesThatInheritBaseType(ProjectItem projectItem, List<TypeWithNamespace> list,string baseType=null)
        {
            if (!(projectItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile))
                return list;
            var codeModel = projectItem.FileCodeModel;
            foreach (var element in codeModel.CodeElements)
            {
                if (!(element is CodeNamespace))
                    continue;
                var ns = element as CodeNamespace;

                foreach (var property in ns.Members)
                {
                    if (!(property is CodeClass type))
                        continue;
                    if (baseType != null)
                    {
                        foreach (var baseClass in (type as CodeType).Bases)
                        {
                            if (!(baseClass is CodeClass bClass))
                                continue;
                            if (bClass.Name == baseType)
                                list.Add(new TypeWithNamespace(type.Name, type.Namespace.FullName));
                        }
                    }
                    else
                    {
                        list.Add(new TypeWithNamespace(type.Name, type.Namespace.FullName));
                    }
                }

            }
            return list;
        }

        public DirectoryInfo GetRootFolder()
        {
            uint itemid = VSConstants.VSITEMID_NIL;
            if (!IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out itemid))
                return null;

            ((IVsProject)hierarchy).GetMkDocument(itemid, out string clickedFolderFullPath);

            return new DirectoryInfo(clickedFolderFullPath).Parent; ;
        }

        private bool IsRootFolderInArea(DirectoryInfo rootFolder)
        {
            return rootFolder.Parent.Name == "Areas";
        }
    }

    public class ComboBoxItem
    {
        public string Name { get; set; }

        public ComboBoxItem(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class TypeWithNamespace
    {
        public string Name { get; set; }
        public string Nmspc { get; set; }

        public TypeWithNamespace(string name, string nmspc)
        {
            Name = name;
            Nmspc = nmspc;
        }
    }

    public class Column
    {
        public string Name {get;set;}
        public string Type {get;set;}
        public bool IsNullable { get; set; } = false;
        public bool IsPrimaryKey { get; set; } = false;
    }
}
