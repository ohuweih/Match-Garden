using UnityEngine;

public sealed class BugReportLink : MonoBehaviour
{
    public void OpenReport()
    {
        Application.OpenURL("https://github.com/ohuweih/Match-Garden/issues/new?template=bug_report.yml");
    }
}
