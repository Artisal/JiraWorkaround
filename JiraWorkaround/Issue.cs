using System.Text;

namespace JiraWorkaround
{
    public class Issue
    {
        public string Id;
        public string Self;
        public string Key;
        public string Summary;
        public string Description;
        public string[] Labels;

        public string GetAllIssueData()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("ID:\t\t");
            sb.AppendLine(Id);

            sb.Append("Self:\t\t");
            sb.AppendLine(Self);

            sb.Append("Key:\t\t");
            sb.AppendLine(Key);

            sb.Append("Summary:\t");
            sb.AppendLine(Summary);

            sb.Append("Description:\t");

            if (string.IsNullOrEmpty(Description))
                sb.AppendLine("<No description>");
            else
                sb.AppendLine(Description);

            sb.Append("Labels:\t\t");

            if (Labels.Length == 0)
                sb.AppendLine("<No labels>");
            else
                sb.AppendLine(string.Join(", ", Labels));

            return sb.ToString();
        }
    }
}
