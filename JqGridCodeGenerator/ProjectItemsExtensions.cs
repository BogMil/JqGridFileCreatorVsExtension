using EnvDTE;

namespace JqGridCodeGenerator
{
    public static class ProjectItemsExtensions
    {
        public static ProjectItem GetProjectItemByName(this Project project, string itemName)
        {
            foreach(ProjectItem projectItem in project.ProjectItems)
            {
                if (projectItem.Name == itemName)
                    return projectItem;
            }
            return null;
        }

        public static ProjectItem GetProjectItemByName(this ProjectItem project, string itemName)
        {
            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                if (projectItem.Name == itemName)
                    return projectItem;
            }
            return null;
        }
    }
}
