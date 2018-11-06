//DUSANRAZVOJ-PC\SQLEXPRESS

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using EnvDTE;
using JqGridCodeGenerator.Commands;
using JqGridCodeGenerator.T4Templates;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextTemplating;
using Constants = EnvDTE.Constants;

namespace JqGridCodeGenerator.ViewModel
{
    public class ChooseDataBaseViewModel : BaseViewModel
    {
        public ICommand PopulateComboBoxWithDatabasesCommand { get; set; }
        public ICommand HandleCredentialsFormVisibilityCommand { get; set; }
        public ICommand HandleControllersComboBoxVisibilityCommand { get; set; }
        public ICommand HandleRepositoriesComboBoxVisibilityCommand { get; set; }
        public ICommand PopulateComboBoxWithTables { get; set; }
        public ICommand CreateFilesCommand { get; set; }

        public List<Column> Columns = new List<Column>();

        public List<string> AddedFolders;

        public ChooseDataBaseViewModel()
        {
            PopulateComboBoxWithDatabasesCommand = new CustomCommand(GetDatabaseNames);
            HandleCredentialsFormVisibilityCommand =
                new CustomCommand(HandleCredentialsFormVisibility);
            HandleControllersComboBoxVisibilityCommand =
                new CustomCommand(HandleControllersComboBoxVisibility);
            HandleRepositoriesComboBoxVisibilityCommand =
                new CustomCommand(HandleRepositoriesComboBoxVisibility);
            PopulateComboBoxWithTables = new CustomCommand(PopulateWithTables);
            CreateFilesCommand = new CustomCommand(CreateFiles);

            AddedFolders = new List<string>();
            GetControllers();
            GetServices();
            GetRepositories();

            var message =
                "Kreirani su sledeci folderi koji su bitni za rad GenericCSR framework-a:\n-----------------------\n";
            foreach (var addedFolder in AddedFolders)
                message += addedFolder + "\n-----------------------\n";

            if (AddedFolders.Count > 0)
                MessageBox.Show(message, "Informacija");

            IsUseBaseControllerEnabled = BaseControllers.Count > 0;
            IsUseBaseRepositoryEnabled = BaseRepositories.Count > 0;
        }

        private Column _primaryKeyColumn;
        private List<TypeWithNamespace> _controllersWithNamespaces;
        private List<TypeWithNamespace> _repositoriesWithNamespaces;
        private string _baseControllerNamespace;
        private string _baseRepositoryNamespace;

        private CollectionView _databaseEntries;
        public CollectionView DatabaseEntries
        {
            get => _databaseEntries;
            set
            {
                _databaseEntries = value;
                OnPropertyChanged();
            }
        }

        private string _databaseEntry;
        public string DatabaseEntry
        {
            get => _databaseEntry;
            set
            {
                if (_databaseEntry == value) return;
                _databaseEntry = value;
                OnPropertyChanged();
                PopulateComboBoxWithTables.Execute(null);
            }
        }

        private CollectionView _tables;
        public CollectionView Tables
        {
            get => _tables;
            set
            {
                _tables = value;
                OnPropertyChanged();
            }
        }

        private string _table;
        public string Table
        {
            get => _table;
            set
            {
                if (_table == value) return;
                _table = value;
                GetColumns();
                OnPropertyChanged();
            }
        }

        private CollectionView _baseControllers;
        public CollectionView BaseControllers
        {
            get => _baseControllers;
            set
            {
                _baseControllers = value;
                OnPropertyChanged();
            }
        }

        private string _baseController;
        public string BaseController
        {
            get => _baseController;
            set
            {
                if (_baseController == value) return;
                _baseController = value;
                _baseControllerNamespace = _controllersWithNamespaces.GetNamespaceForType(value);
                OnPropertyChanged();
            }
        }

        private CollectionView _baseRepositories;
        public CollectionView BaseRepositories
        {
            get => _baseRepositories;
            set
            {
                _baseRepositories = value;
                OnPropertyChanged();
            }
        }

        private string _baseRepository;
        public string BaseRepository
        {
            get => _baseRepository;
            set
            {
                if (_baseRepository == value) return;
                _baseRepository = value;
                _baseRepositoryNamespace = _repositoriesWithNamespaces.GetNamespaceForType(value);
                OnPropertyChanged();
            }
        }

        private string _sqlServerName = "Sql_Server";
        public string SqlServerName
        {
            get => _sqlServerName;
            set
            {
                if (value != _sqlServerName)
                {
                    _sqlServerName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _username = "Username";
        public string Username
        {
            get => _username;
            set
            {
                if (value == _username) return;
                _username = value;
                OnPropertyChanged();
            }
        }

        private string _password = "Password";
        public string Password
        {
            get => _password;
            set
            {
                if (value != _password)
                {
                    _password = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _baseName = "BaseName";
        public string BaseName
        {
            get => _baseName;
            set
            {
                if (value != _baseName)
                {
                    _baseName = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _useWindowsIdentity;
        public bool UseWindowsIdentity
        {
            get => _useWindowsIdentity;
            set
            {
                if (value != _useWindowsIdentity)
                {
                    _useWindowsIdentity = value;
                    OnPropertyChanged();
                    HandleCredentialsFormVisibilityCommand.Execute(null);
                }
            }
        }

        private bool _useBaseController;
        public bool UseBaseController
        {
            get => _useBaseController;
            set
            {
                if (value != _useBaseController)
                {
                    _useBaseController = value;
                    OnPropertyChanged();
                    HandleControllersComboBoxVisibilityCommand.Execute(null);
                }
            }
        }

        private bool _isUseBaseControllerEnabled;
        public bool IsUseBaseControllerEnabled
        {
            get => _isUseBaseControllerEnabled;
            set
            {
                if (value != _isUseBaseControllerEnabled)
                {
                    _isUseBaseControllerEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isUseBaseRepositoryEnabled;
        public bool IsUseBaseRepositoryEnabled
        {
            get => _isUseBaseRepositoryEnabled;
            set
            {
                if (value != _isUseBaseRepositoryEnabled)
                {
                    _isUseBaseRepositoryEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _useBaseRepository;
        public bool UseBaseRepository
        {
            get => _useBaseRepository;
            set
            {
                if (value == _useBaseRepository) return;
                _useBaseRepository = value;
                OnPropertyChanged();
                HandleRepositoriesComboBoxVisibilityCommand.Execute(null);
            }
        }

        public void GetDatabaseNames(object parameter)
        {
            var list = new List<ComboBoxItem>();

            using (var con = new SqlConnection(CreateConnectionString()))
            {
                con.Open();

                using (var cmd = new SqlCommand("SELECT name from sys.databases", con))
                {
                    using (IDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                            list.Add(new ComboBoxItem(dr[0].ToString()));
                        DatabaseEntries = new CollectionView(list);
                    }
                }

                con.Close();
            }
        }

        public void PopulateWithTables(object parameter)
        {
            var list = new List<ComboBoxItem>();
            var conString = CreateConnectionString();

            using (var con = new SqlConnection(conString))
            {
                con.Open();

                using (var cmd =
                    new SqlCommand(
                        "SELECT TABLE_NAME FROM " + DatabaseEntry +
                        ".INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", con))
                {
                    using (IDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read()) list.Add(new ComboBoxItem(dr[0].ToString()));
                        Tables = new CollectionView(list);
                    }
                }

                con.Close();
            }
        }

        public void GetColumns()
        {
            using (var conn = new SqlConnection(CreateConnectionString()))
            {
                conn.Open();
                var columnRestrictions = new string[4];
                columnRestrictions[0] = DatabaseEntry;
                columnRestrictions[2] = Table;

                var shemaTable = conn.GetSchema("Columns", columnRestrictions);

                var columns = (from info in shemaTable.AsEnumerable()
                    select new Column
                    {
                        Name = info["COLUMN_NAME"].ToString(),
                        Type = info["DATA_TYPE"].ToString(),
                        IsNullable = info["IS_NULLABLE"].ToString() == "YES"
                    }).ToList();

                Columns = new List<Column>();
                foreach (var column in columns)
                    Columns.Add(column);

                columnRestrictions[3] = "PK_" + Table;

                shemaTable = conn.GetSchema("IndexColumns", columnRestrictions);
                var primaryKey = (from info in shemaTable.AsEnumerable()
                    select new
                    {
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
                    MessageBox.Show("Tabela nema primarni kljuc. Morate izabrati tabelu koja ima primarni kljuc",
                        "Informacija");
                }

                conn.Close();
            }
        }

        private string CreateConnectionString()
        {
            var connBuilder = new SqlConnectionStringBuilder
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
            if (obj is StackPanel credentialFormStackPanel)
                credentialFormStackPanel.IsEnabled = UseWindowsIdentity != true;
        }

        private void HandleControllersComboBoxVisibility(object obj)
        {
            if (obj is ComboBox controllersComboBox) controllersComboBox.IsEnabled = UseBaseController;
        }

        private void HandleRepositoriesComboBoxVisibility(object obj)
        {
            if (obj is ComboBox servicesComboBox) servicesComboBox.IsEnabled = UseBaseRepository;
        }

        public void CreateFiles(object parametar)
        {
            if (string.IsNullOrEmpty(Table))
            {
                MessageBox.Show("Изабери табелу", "Informacija");
                return;
            }

            if (_primaryKeyColumn == null)
            {
                MessageBox.Show(
                    "Tabela nema primarni kljuc. Nije moguce koristiti GenericCRS za tabele koje nemaju primarni kljuc",
                    "Informacija");
                return;
            }

            var rootFolder = GetRootFolder();

            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            var activeProject = GetActiveProject(dte);

            CreateIServiceFile(activeProject, rootFolder);
            CreateServiceFile(activeProject, rootFolder);
            CreateIRepositoryFile(activeProject, rootFolder);
            CreateRepositoryFile(activeProject, rootFolder);
            CreateControllerFile(activeProject, rootFolder);
            CreateModelFile(activeProject, rootFolder);

            JqGridCodeGeneratorWindow.Instance.Close();
        }

        private void CreateIServiceFile(Project activeProject, DirectoryInfo rootFolder)
        {
            var filePath = rootFolder.FullName + "\\Services\\CRUD\\Interfaces\\I" + BaseName + "Service.cs";
            var file = File.Create(filePath);
            file.Close();
            var page = new IServiceTemplate
            {
                Session = new TextTemplatingSession
                {
                    ["baseName"] = BaseName, ["baseNamespace"] = GetBaseNamespace(rootFolder)
                }
            };
            page.Initialize();

            var pageContent = page.TransformText();
            File.WriteAllText(filePath, pageContent);

            activeProject.ProjectItems.AddFromFile(filePath);

            var itemOp = activeProject.DTE.ItemOperations;
            itemOp.OpenFile(filePath, Constants.vsViewKindTextView);
        }

        private void CreateIRepositoryFile(Project activeProject, DirectoryInfo rootFolder)
        {
            var filePath = rootFolder.FullName + "\\Repositories\\Interfaces\\I" + BaseName + "Repository.cs";
            var file = File.Create(filePath);
            file.Close();
            var page = new IRepositoryTemplate
            {
                Session = new TextTemplatingSession
                {
                    ["baseName"] = BaseName,
                    ["tableName"] = Table,
                    ["baseNamespace"] = GetBaseNamespace(rootFolder)
                }
            };
            page.Initialize();

            var pageContent = page.TransformText();
            File.WriteAllText(filePath, pageContent);

            activeProject.ProjectItems.AddFromFile(filePath);

            var itemOp = activeProject.DTE.ItemOperations;
            itemOp.OpenFile(filePath, Constants.vsViewKindTextView);
        }

        private void CreateRepositoryFile(Project activeProject, DirectoryInfo rootFolder)
        {
            var filePath = rootFolder.FullName + "\\Repositories\\" + BaseName + "Repository.cs";
            var file = File.Create(filePath);
            file.Close();
            var page = new RepositoryTemplate
            {
                Session = new TextTemplatingSession
                {
                    ["baseName"] = BaseName,
                    ["tableName"] = Table,
                    ["baseNamespace"] = GetBaseNamespace(rootFolder),
                    ["primaryKeyName"] = _primaryKeyColumn.Name,
                    ["useBaseRepository"] = _useBaseRepository,
                    ["baseRepositoryNamespace"] = _baseRepositoryNamespace,
                    ["baseRepositoryName"] = BaseRepository
                }
            };
            page.Initialize();

            var pageContent = page.TransformText();
            File.WriteAllText(filePath, pageContent);

            activeProject.ProjectItems.AddFromFile(filePath);

            var itemOp = activeProject.DTE.ItemOperations;
            itemOp.OpenFile(filePath, Constants.vsViewKindTextView);
        }

        private void CreateControllerFile(Project activeProject, DirectoryInfo rootFolder)
        {
            var filePath = rootFolder.FullName + "\\Controllers\\" + BaseName + "Controller.cs";
            var file = File.Create(filePath);
            file.Close();
            var page = new ControllerTemplate
            {
                Session = new TextTemplatingSession
                {
                    ["baseName"] = BaseName,
                    ["baseNamespace"] = GetBaseNamespace(rootFolder),
                    ["useBaseController"] = _useBaseController,
                    ["baseControllerNamespace"] = _baseControllerNamespace,
                    ["baseControllerName"] = BaseController
                }
            };
            page.Initialize();

            var pageContent = page.TransformText();
            File.WriteAllText(filePath, pageContent);

            activeProject.ProjectItems.AddFromFile(filePath);

            var itemOp = activeProject.DTE.ItemOperations;
            itemOp.OpenFile(filePath, Constants.vsViewKindTextView);
        }

        private void CreateModelFile(Project activeProject, DirectoryInfo rootFolder)
        {
            var filePath = rootFolder.FullName + "\\Models\\" + BaseName + ".cs";
            var file = File.Create(filePath);
            file.Close();
            var page = new ModelTemplate
            {
                Session = new TextTemplatingSession
                {
                    ["baseName"] = BaseName,
                    ["baseNamespace"] = GetBaseNamespace(rootFolder),
                    ["tableName"] = Table,
                    ["primaryKeyColumn"] = _primaryKeyColumn,
                    ["columns"] = Columns
                }
            };

            page.Initialize();

            var pageContent = page.TransformText();
            File.WriteAllText(filePath, pageContent);

            activeProject.ProjectItems.AddFromFile(filePath);

            var itemOp = activeProject.DTE.ItemOperations;
            itemOp.OpenFile(filePath, Constants.vsViewKindTextView);
        }

        private void CreateServiceFile(Project activeProject, DirectoryInfo rootFolder)
        {
            var filePath = rootFolder.FullName + "\\Services\\CRUD\\" + BaseName + "Service.cs";
            var file = File.Create(filePath);
            file.Close();
            var page = new ServiceTemplate
            {
                Session = new TextTemplatingSession
                {
                    ["baseName"] = BaseName,
                    ["baseNamespace"] = GetBaseNamespace(rootFolder),
                    ["tableName"] = Table
                }
            };
            page.Initialize();

            var pageContent = page.TransformText();
            File.WriteAllText(filePath, pageContent);

            activeProject.ProjectItems.AddFromFile(filePath);

            var itemOp = activeProject.DTE.ItemOperations;
            itemOp.OpenFile(filePath, Constants.vsViewKindTextView);
        }

        private string GetBaseNamespace(DirectoryInfo rootFolder)
        {
            if (!IsRootFolderInArea(rootFolder)) return rootFolder.Name;
            if (rootFolder.Parent == null) return rootFolder.Name;
            if (rootFolder.Parent.Parent != null)
                return rootFolder.Parent.Parent.Name + "." + rootFolder.Parent.Name + "." + rootFolder.Name;
            return rootFolder.Name;
        }

        public static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
        {
            hierarchy = null;
            itemid = VSConstants.VSITEMID_NIL;

            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null) return false;

            var hierarchyPtr = IntPtr.Zero;
            var selectionContainerPtr = IntPtr.Zero;

            try
            {
                var hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out var multiItemSelect,
                    out selectionContainerPtr);

                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                    return false;

                // multiple items are selected
                if (multiItemSelect != null) return false;

                // there is a hierarchy root node selected, thus it is not a single item inside a project

                if (itemid == VSConstants.VSITEMID_ROOT) return false;

                hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null) return false;

                return !ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out _));

                // if we got this far then there is a single project item selected
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero) Marshal.Release(selectionContainerPtr);

                if (hierarchyPtr != IntPtr.Zero) Marshal.Release(hierarchyPtr);
            }
        }

        public Project GetActiveProject(DTE dte)
        {
            if (dte.ActiveSolutionProjects is Array activeProjects) return (Project) activeProjects.GetValue(0);
            throw new Exception("GetActiveProject is null");
        }

        public void GetControllers()
        {
            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            var activeProject = GetActiveProject(dte);

            var rootFolder = GetRootFolder();

            var listOfTypesWithNamespaces = new List<TypeWithNamespace>();
            var controllerFolder = activeProject.GetProjectItemByName("Controllers");
            foreach (ProjectItem controller in controllerFolder.ProjectItems)
                listOfTypesWithNamespaces = PopulateListWithTypesThatInheritBaseType(controller,
                    listOfTypesWithNamespaces /*, "GenericController"*/);

            if (IsRootFolderInArea(rootFolder))
            {
                var areasFolder = activeProject.GetProjectItemByName("Areas");
                var areasRootFolder = areasFolder.GetProjectItemByName(rootFolder.Name);
                var areasControllerFolder = areasRootFolder.GetProjectItemByName("Controllers");
                foreach (ProjectItem controller in areasControllerFolder.ProjectItems)
                    listOfTypesWithNamespaces = PopulateListWithTypesThatInheritBaseType(controller,
                        listOfTypesWithNamespaces /*,"GenericController"*/);
            }

            var listOfControllerName = listOfTypesWithNamespaces.Select(x => x.Name).ToList();
            var comboBoxList = new List<ComboBoxItem>();

            foreach (var controllerName in listOfControllerName) comboBoxList.Add(new ComboBoxItem(controllerName));
            _controllersWithNamespaces = listOfTypesWithNamespaces;
            BaseControllers = new CollectionView(comboBoxList);
        }

        public void GetServices()
        {
            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            var activeProject = GetActiveProject(dte);

            var rootFolder = GetRootFolder();

            var servicesFolder = activeProject.GetProjectItemByName("Services");

            if (servicesFolder == null)
            {
                servicesFolder = activeProject.ProjectItems.AddFolder("Services");
                var crudFolder = servicesFolder.ProjectItems.AddFolder("CRUD");
                crudFolder.ProjectItems.AddFolder("Interfaces");

                if (rootFolder.Parent?.Parent != null)
                {
                    AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Services");
                    AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Services\\CRUD");
                    AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Services\\CRUD\\Interfaces");
                }
            }
            else
            {
                var crudFolder = servicesFolder.GetProjectItemByName("CRUD");
                if (crudFolder == null)
                {
                    crudFolder = servicesFolder.ProjectItems.AddFolder("CRUD");
                    crudFolder.ProjectItems.AddFolder("Interfaces");

                    if (rootFolder.Parent?.Parent != null)
                    {
                        AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Services\\CRUD");
                        AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Services\\CRUD\\Interfaces");
                    }
                }

                var interfacesFolder = crudFolder.GetProjectItemByName("Interfaces");
                if (interfacesFolder == null)
                {
                    crudFolder.ProjectItems.AddFolder("Interfaces");
                    if (rootFolder.Parent?.Parent != null)
                        AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Services\\CRUD\\Interfaces");
                }
            }

            if (IsRootFolderInArea(rootFolder))
            {
                var areasFolder = activeProject.GetProjectItemByName("Areas");
                var rootFolderInAreas = areasFolder.GetProjectItemByName(rootFolder.Name);
                var servicesFolderInRoot = rootFolderInAreas.GetProjectItemByName("Services");
                if (servicesFolderInRoot == null)
                {
                    servicesFolderInRoot = rootFolderInAreas.ProjectItems.AddFolder("Services");
                    var crudFolder = servicesFolderInRoot.ProjectItems.AddFolder("CRUD");
                    crudFolder.ProjectItems.AddFolder("Interfaces");

                    if (rootFolder.Parent?.Parent == null) return;
                    AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" +
                                     rootFolder.Name + "\\Services");
                    AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" +
                                     rootFolder.Name + "\\Services\\CRUD");
                    AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" +
                                     rootFolder.Name + "\\Services\\CRUD\\Interfaces");
                }
                else
                {
                    var crudFolder = servicesFolderInRoot.GetProjectItemByName("CRUD");
                    if (crudFolder == null)
                    {
                        crudFolder = servicesFolderInRoot.ProjectItems.AddFolder("CRUD");
                        crudFolder.ProjectItems.AddFolder("Interfaces");

                        if (rootFolder.Parent?.Parent != null)
                        {
                            AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" +
                                             rootFolder.Name + "\\Services\\CRUD");
                            AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" +
                                             rootFolder.Name + "\\Services\\CRUD\\Interfaces");
                        }
                    }

                    var interfacesFolder = crudFolder.GetProjectItemByName("Interfaces");
                    if (interfacesFolder != null) return;
                    crudFolder.ProjectItems.AddFolder("Interfaces");

                    if (rootFolder.Parent?.Parent != null)
                        AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" +
                                         rootFolder.Name + "\\Services\\CRUD\\Interfaces");
                }
            }
        }

        public void GetRepositories()
        {
            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            var activeProject = GetActiveProject(dte);

            var rootFolder = GetRootFolder();

            var listOfTypesWithNamespaces = new List<TypeWithNamespace>();
            var repositoriesFolder = activeProject.GetProjectItemByName("Repositories");

            if (repositoriesFolder == null)
            {
                repositoriesFolder = activeProject.ProjectItems.AddFolder("Repositories");
                repositoriesFolder.ProjectItems.AddFolder("Interfaces");
                if (rootFolder.Parent?.Parent != null)
                {
                    AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Repositories");
                    AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Repositories\\Interfaces");
                }
            }
            else
            {
                var interfacesFolder = repositoriesFolder.GetProjectItemByName("Interfaces");
                if (interfacesFolder == null)
                {
                    repositoriesFolder.ProjectItems.AddFolder("Interfaces");
                    if (rootFolder.Parent?.Parent != null)
                        AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\Repositories\\Interfaces");
                }

                foreach (ProjectItem repository in repositoriesFolder.ProjectItems)
                    listOfTypesWithNamespaces = PopulateListWithTypesThatInheritBaseType(repository,
                        listOfTypesWithNamespaces /*, "GenericRepository"*/);
            }

            if (IsRootFolderInArea(rootFolder))
            {
                var areasFolder = activeProject.GetProjectItemByName("Areas");
                var rootFolderInAreas = areasFolder.GetProjectItemByName(rootFolder.Name);
                var repositoriesFolderInRoot = rootFolderInAreas.GetProjectItemByName("Repositories");
                if (repositoriesFolderInRoot == null)
                {
                    repositoriesFolderInRoot = rootFolderInAreas.ProjectItems.AddFolder("Repositories");
                    repositoriesFolderInRoot.ProjectItems.AddFolder("Interfaces");

                    if (rootFolder.Parent?.Parent != null)
                    {
                        AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" +
                                         rootFolder.Name + "\\Repositories");
                        AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" +
                                         rootFolder.Name + "\\Repositories\\Interfaces");
                    }
                }
                else
                {
                    var interfacesFolder = repositoriesFolderInRoot.GetProjectItemByName("Interfaces");
                    if (interfacesFolder == null)
                    {
                        repositoriesFolderInRoot.ProjectItems.AddFolder("Interfaces");
                        if (rootFolder.Parent?.Parent != null)
                            AddedFolders.Add(rootFolder.Parent.Parent.Name + "\\" + rootFolder.Parent.Name + "\\" +
                                             rootFolder.Name + "\\Repositories\\Interfaces");
                    }

                    foreach (ProjectItem repository in repositoriesFolderInRoot.ProjectItems)
                        listOfTypesWithNamespaces = PopulateListWithTypesThatInheritBaseType(repository,
                            listOfTypesWithNamespaces /*, "GenericRepository"*/);
                }
            }

            var listOfRepositoryName = listOfTypesWithNamespaces.Select(x => x.Name).ToList();
            var comboBoxList = new List<ComboBoxItem>();

            foreach (var repositoryName in listOfRepositoryName) comboBoxList.Add(new ComboBoxItem(repositoryName));
            _repositoriesWithNamespaces = listOfTypesWithNamespaces;
            BaseRepositories = new CollectionView(comboBoxList);
        }


        public List<TypeWithNamespace> PopulateListWithTypesThatInheritBaseType(ProjectItem projectItem,
            List<TypeWithNamespace> list, string baseType = null)
        {
            if (projectItem.Kind != Constants.vsProjectItemKindPhysicalFile)
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
                        var codeElements = (type as CodeType)?.Bases;

                        if (codeElements == null) continue;
                        foreach (var baseClass in codeElements)
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
            if (!IsSingleProjectItemSelection(out var hierarchy, out var itemid))
                return null;

            ((IVsProject) hierarchy).GetMkDocument(itemid, out var clickedFolderFullPath);

            return new DirectoryInfo(clickedFolderFullPath).Parent;
        }

        private static bool IsRootFolderInArea(DirectoryInfo rootFolder)
        {
            return rootFolder.Parent != null && rootFolder.Parent.Name == "Areas";
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
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
    }
}