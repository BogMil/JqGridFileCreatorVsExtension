using JqGridCodeGenerator.Commands;
using JqGridCodeGenerator.View.Pages;
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
        public ICommand PopulateComboBoxWithTables { get; set; }
        public ICommand CreateFilesCommand { get; set; }

        public List<Column> Columns = new List<Column>() {
            new Column {DataType="test1",IsNullable=false,IsPrimaryKey=false,Name="Column1" },
            new Column {DataType="test1",IsNullable=false,IsPrimaryKey=false,Name="Column2" },
            new Column {DataType="test1",IsNullable=false,IsPrimaryKey=false,Name="Column3" }
        };

        private void onPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public List<string> AddedFolders;

        private string _SqlServerName = "Enter Sql Server Name";
        private string _Username = "Enter username";
        private string _Password = "Enter password";
        private bool _UseWindowsIdentity = false;

        public ChooseDataBaseViewModel()
        {
            PopulateComboBoxWithDatabasesCommand = new CustomCommand(Populate, CanExecuteCommand);
            HandleCredentialsFormVisibilityCommand = new CustomCommand(HandleCredentialsFormVisibility, CanExecuteCommand);
            PopulateComboBoxWithTables = new CustomCommand(PopulateWithTables, CanExecuteCommand);
            CreateFilesCommand = new CustomCommand(CreateFiles, CanExecuteCommand);

            AddedFolders = new List<string>();
            GetControllers();
            GetServices(); 
            CreateRepositoriesFoldersIfNeeded();

            string message = "Kreirani su sledeci folderi koji su bitni za rad GenericCSR framework-a:\n-----------------------\n";
            foreach (var addedFolder in AddedFolders)
                message += addedFolder+"\n-----------------------\n";

            if(AddedFolders.Count>0)
                MessageBox.Show(message,"Informacija");
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

        private CollectionView _controllers;
        private string _controller;

        public CollectionView Controllers
        {
            get { return _controllers; }
            set
            {
                _controllers = value;
                onPropertyChanged("Controllers");
            }
        }

        public string Controller
        {
            get { return _controller; }
            set
            {
                if (_controller == value) return;
                _controller = value;
                onPropertyChanged("Controller");
            }
        }

        private CollectionView _services;
        private string _service;

        public CollectionView Services
        {
            get { return _services; }
            set
            {
                _services = value;
                onPropertyChanged("Services");
            }
        }

        public string Service
        {
            get { return _service; }
            set
            {
                if (_service == value) return;
                _service = value;
                onPropertyChanged("Service");
            }
        }

        private CollectionView _repositories;
        private string _repository;

        public CollectionView Repositories
        {
            get { return _repositories; }
            set
            {
                _repositories = value;
                onPropertyChanged("Repositories");
            }
        }

        public string Repository
        {
            get { return _repository; }
            set
            {
                if (_service == value) return;
                _repository = value;
                onPropertyChanged("Repository");
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
            List<ComboBoxItem> list = new List<ComboBoxItem>();
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
                        DataType = info["DATA_TYPE"].ToString(),
                        //IsNullable = info["IS_NULLABLE"]
                    }).ToList();

                columnRestrictions[3] = "PK_"+Table;

                DataTable departmentIDSchemaTable = conn.GetSchema("IndexColumns", columnRestrictions);
                var primaryKey = (from info in departmentIDSchemaTable.AsEnumerable()
                    select new{
                        ColumnName = info["COLUMN_NAME"].ToString()
                    }).ToList().FirstOrDefault();
                if(primaryKey!=null)
                    foreach(var column in columns)
                    {
                        if (column.Name == primaryKey.ColumnName)
                            column.IsPrimaryKey = true;
                    }

                Columns = columns;

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

        public void CreateFiles(object parametar)
        {
            var rootFolder = GetRootFolder();

            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;

            var activeProject = GetActiveProject(dte);

            var file1Path = rootFolder.FullName + "\\Controllers\\testFile.cs";
            var myFile = File.Create(file1Path);
            myFile.Close();
            TestTemplate page = new TestTemplate();
            page.Session = new TextTemplatingSession
            {
                ["name"] = "testDaLiRadi",
                ["controllerNamespace"] = "controllerNamespaceTebra",
                ["columns"] = Columns
            };
            page.Initialize();
            String pageContent = page.TransformText();
            File.WriteAllText(file1Path, pageContent);
            var file2Path = rootFolder.FullName + "\\Controllers\\testFile2.cs";
            var myFile2 = File.Create(file2Path);
            myFile2.Close();

            activeProject.ProjectItems.AddFromFile(file1Path);
            activeProject.ProjectItems.AddFromFile(file2Path);

            ItemOperations ItemOp = dte.ItemOperations;
            ItemOp.OpenFile(file1Path, EnvDTE.Constants.vsViewKindTextView);

            CreateIServiceFile(activeProject,rootFolder);
            CreateServiceFile(activeProject, rootFolder);

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
                ["useCustomBaseService"]=false
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

        private static Project GetCurrentProjectFromSolution(string projFullName, IVsSolution solution)
        {
            foreach (Project project in GetProjects(solution))
            {
                if (project.FullName == projFullName)
                    return project;
            }
            throw new Exception("Ne postoji projekad na lokaciji : "+projFullName);
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

        public static IEnumerable<Project> GetProjects(IVsSolution solution)
        {
            foreach (IVsHierarchy hier in GetProjectsInSolution(solution))
            {
                EnvDTE.Project project = GetDTEProject(hier);
                if (project != null)
                    yield return project;
            }
        }

        public static IEnumerable<IVsHierarchy> GetProjectsInSolution(IVsSolution solution)
        {
            return GetProjectsInSolution(solution, __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION);
        }

        public static IEnumerable<IVsHierarchy> GetProjectsInSolution(IVsSolution solution, __VSENUMPROJFLAGS flags)
        {
            if (solution == null)
                yield break;

            IEnumHierarchies enumHierarchies;
            Guid guid = Guid.Empty;
            solution.GetProjectEnum((uint)flags, ref guid, out enumHierarchies);
            if (enumHierarchies == null)
                yield break;

            IVsHierarchy[] hierarchy = new IVsHierarchy[1];
            uint fetched;
            while (enumHierarchies.Next(1, hierarchy, out fetched) == VSConstants.S_OK && fetched == 1)
            {
                if (hierarchy.Length > 0 && hierarchy[0] != null)
                    yield return hierarchy[0];
            }
        }

        public static Project GetDTEProject(IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
                throw new ArgumentNullException("hierarchy");

            object obj;
            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out obj);
            return obj as EnvDTE.Project;
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

            List<ComboBoxItem> list = new List<ComboBoxItem>();
            var controllerFolder = activeProject.GetProjectItemByName("Controllers");
            foreach (ProjectItem controller in controllerFolder.ProjectItems)
            {
                list= PopulateListWithTypesThatInheritBaseType(controller,list);
            }

            if (IsRootFolderInArea(rootFolder))
            {
                var areasFolder = activeProject.GetProjectItemByName("Areas");
                var areasRootFolder = areasFolder.GetProjectItemByName(rootFolder.Name);
                var areasControllerFolder = areasRootFolder.GetProjectItemByName("Controllers");
                foreach (ProjectItem controller in areasControllerFolder.ProjectItems)
                {
                    list = PopulateListWithTypesThatInheritBaseType(controller, list);
                }
            }
            Controllers = new CollectionView(list);
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
                else
                {
                    foreach (ProjectItem service in crudFolder.ProjectItems)
                    {
                        list = PopulateListWithTypesThatInheritBaseType(service, list);
                    }
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
                    else
                    {
                        foreach (ProjectItem service in crudFolder.ProjectItems)
                        {
                            list = PopulateListWithTypesThatInheritBaseType(service, list);
                        }
                    }
                }
            }
            Services = new CollectionView(list);
        }

        public void CreateRepositoriesFoldersIfNeeded()
        {
            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            var activeProject = GetActiveProject(dte);
            IVsSolution solution = (IVsSolution)Package.GetGlobalService(typeof(IVsSolution));

            var rootFolder = GetRootFolder();

            List<ComboBoxItem> list = new List<ComboBoxItem>();
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
                }
            }
        }


        public List<ComboBoxItem> PopulateListWithTypesThatInheritBaseType(ProjectItem projectItem, List<ComboBoxItem> list,string baseType=null)
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
                                list.Add(new ComboBoxItem(type.Name));
                        }
                    }
                    else
                    {
                        list.Add(new ComboBoxItem(type.Name));
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

    public class Column
    {
        public string Name {get;set;}
        public string DataType {get;set;}
        public bool IsNullable { get; set; } = false;
        public bool IsPrimaryKey { get; set; } = false;
    }
}
