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


namespace JqGridCodeGenerator.ViewModel
{
    class ChooseDataBaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand PopulateComboBoxWithDatabasesCommand { get; set; }
        public ICommand HandleCredentialsFormVisibilityCommand { get; set; }
        public ICommand PopulateComboBoxWithTables { get; set; }
        public ICommand CreateFilesCommand { get; set; }

        public List<Column> Columns;

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
            CreateFilesCommand = new CustomCommand(CreateFiles, CanExecuteCommand);

            GetControllers();
            GetServices(); 
            GetRepositories();

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
            page.Session = new Microsoft.VisualStudio.TextTemplating.TextTemplatingSession();
            page.Session["name"] = "testDaLiRadi";
            page.Session["controllerNamespace"] = "controllerNamespaceTebra";
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

            JqGridCodeGeneratorWindow.Instance.Close();
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
            var isInArea = rootFolder.Name == "Controllers";

            List<ComboBoxItem> list = new List<ComboBoxItem>();
            var controllerFolder = activeProject.GetProjectItemByName("Controllers");
            foreach (ProjectItem controller in controllerFolder.ProjectItems)
            {
                if (controller.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile)
                {
                    var x = controller.FileCodeModel;
                    foreach(var ele in x.CodeElements)
                    {
                        if (ele is CodeNamespace)
                        {
                            var ns = ele as CodeNamespace;
                            // run through classes
                            foreach (var property in ns.Members)
                            {
                                var type = property as CodeClass;
                                if (type == null)
                                    continue;
                                var z = type.Name;
                                list.Add(new ComboBoxItem(type.Name));
                            }
                        }
                    }
                }
            }

            if (isInArea)
            {
                var areasFolder = activeProject.GetProjectItemByName("Areas");
                var areasControllerFolder = areasFolder.GetProjectItemByName("Controllers");
                foreach(ProjectItem controller in areasControllerFolder.ProjectItems)
                {
                    list.Add(new ComboBoxItem(controller.Name));
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
            var isInArea = rootFolder.Parent.Name == "Areas";

            List<ComboBoxItem> list = new List<ComboBoxItem>();
            var servicesFolder = activeProject.GetProjectItemByName("Services");
            
            if (servicesFolder == null)
            {

                servicesFolder=activeProject.ProjectItems.AddFolder("Services");
                var crudFolder = servicesFolder.ProjectItems.AddFolder("CRUD");
                crudFolder.ProjectItems.AddFolder("Interfaces");
                servicesFolder = crudFolder;
            }
            else
            {
                var crudFolder = servicesFolder.GetProjectItemByName("CRUD");
                if (crudFolder==null)
                {
                    crudFolder=servicesFolder.ProjectItems.AddFolder("CRUD");
                }
                var interfacesFolder = crudFolder.GetProjectItemByName("Interfaces");
                if (interfacesFolder == null)
                {
                    crudFolder.ProjectItems.AddFolder("Interfaces");
                }
                foreach (ProjectItem service in crudFolder.ProjectItems)
                {
                    //dodati samo filove bez foldera
                    list.Add(new ComboBoxItem(service.Name));
                }
            }

            if (isInArea)
            {
                var areasFolder = activeProject.GetProjectItemByName("Areas");
                var rootFolderInAreas = areasFolder.GetProjectItemByName(rootFolder.Name);
                var ServicesFolderInRoot = rootFolderInAreas.GetProjectItemByName("Services");
                if (ServicesFolderInRoot == null)
                {
                    ServicesFolderInRoot=rootFolderInAreas.ProjectItems.AddFolder("Services");
                }
                foreach (ProjectItem service in ServicesFolderInRoot.ProjectItems)
                {
                    list.Add(new ComboBoxItem(service.Name));
                }
            }
            Services = new CollectionView(list);
        }

        public void GetRepositories()
        {
            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            var activeProject = GetActiveProject(dte);
            IVsSolution solution = (IVsSolution)Package.GetGlobalService(typeof(IVsSolution));

            var rootFolder = GetRootFolder();
            var isInArea = rootFolder.Name == "Areas";

            List<ComboBoxItem> list = new List<ComboBoxItem>();
            var repositoriesFolder = activeProject.GetProjectItemByName("Repositories");

            if (repositoriesFolder == null)
            {
                repositoriesFolder=activeProject.ProjectItems.AddFolder("Repositories");
            }

            foreach (ProjectItem controller in repositoriesFolder.ProjectItems)
            {
                list.Add(new ComboBoxItem(controller.Name));
            }

            if (isInArea)
            {
                var areasFolder = activeProject.GetProjectItemByName("Areas");
                var rootFolderInAreas = areasFolder.GetProjectItemByName(rootFolder.Name);
                var areasRepositoriesFolder = rootFolderInAreas.GetProjectItemByName("Repositories");
                if (areasRepositoriesFolder == null)
                {
                    areasRepositoriesFolder.ProjectItems.AddFolder("Repositories");
                }
                foreach (ProjectItem repository in areasRepositoriesFolder.ProjectItems)
                {
                    list.Add(new ComboBoxItem(repository.Name));
                }
            }
            Repositories = new CollectionView(list);
        }

        public DirectoryInfo GetRootFolder()
        {
            uint itemid = VSConstants.VSITEMID_NIL;
            if (!IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out itemid))
                return null;

            ((IVsProject)hierarchy).GetMkDocument(itemid, out string clickedFolderFullPath);

            return new DirectoryInfo(clickedFolderFullPath).Parent; ;
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
