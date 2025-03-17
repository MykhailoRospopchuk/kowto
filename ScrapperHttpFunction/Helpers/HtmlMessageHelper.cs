namespace ScrapperHttpFunction.Helpers;

using System.Text;
using CosmoDatabase.Entities;

public class HtmlMessageHelper
{
    public static string BuildHtml(List<JobInfo> jobs)
    {
        var sb = new StringBuilder();

        foreach (var job in jobs)
        {
            sb.AppendLine("<div style=\"border: 1px solid #ccc; padding: 10px; margin: 10px;\">");
            sb.AppendLine($"  <h2 style=\"margin-top:0;\">{job.Title}</h2>");
            sb.AppendLine($"  <p><strong>Date:</strong> {job.Date}</p>");
            sb.AppendLine($"  <p><strong>Company:</strong> {job.CompanyName}</p>");
            sb.AppendLine($"  <p><strong>URL:</strong> <a href=\"{job.Url}\" target=\"_blank\">{job.Url}</a></p>");
            sb.AppendLine($"  <p><strong>Id:</strong> {job.Id}</p>");
            sb.AppendLine($"  <p><strong>Hash:</strong> {job.Hash}</p>");
            sb.AppendLine("</div>");
        }

        return sb.ToString();
    }

    public static string BuildHtml(int count, string reportUrl)
    {
        var template =
            $"""
             <div style=\"border: 1px solid #ccc; padding: 10px; margin: 10px;\">"
                <h2 style="margin-top:0;"><strong>{DateOnly.FromDateTime(DateTime.UtcNow)}</strong> - Today</h2>
                <h2 style="margin-top:0;"><strong>{count}</strong> - new records were added in the past 24 hours</h2>
                <p><strong>Report URL:</strong> <a href="{reportUrl}" target="_blank">{reportUrl}</a></p>
             </div>
             """;

        return template;
    }
}