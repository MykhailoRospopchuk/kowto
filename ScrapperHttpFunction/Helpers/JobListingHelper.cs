using System;
using HtmlAgilityPack;
using ScrapperHttpFunction.Models;

namespace ScrapperHttpFunction.Helpers;

public class JobListingHelper
{
    public static List<JobListing> FetchJobListings(string rawPage)
    {
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(rawPage);

        List<JobListing> jobList = new List<JobListing>();

        // Find the main vacancy container
        var vacancyList = doc.DocumentNode.SelectSingleNode("//div[@class='l-items' and @id='vacancyListId']");
        if (vacancyList != null)
        {
            var jobNodes = vacancyList.SelectNodes(".//li[contains(@class, 'l-vacancy')]");

            if (jobNodes != null)
            {
                foreach (var job in jobNodes)
                {
                    string date = job.SelectSingleNode(".//div[@class='date']")?.InnerText.Trim() ?? "N/A";
                    var vacancyTitleNode = job.SelectSingleNode(".//a[@class='vt']");
                    string title = vacancyTitleNode?.InnerText.Trim() ?? "N/A";
                    string jobUrl = vacancyTitleNode?.GetAttributeValue("href", "#") ?? "#";
                    var companyName = job.SelectSingleNode(".//a[@class='company']")?.InnerText.Trim().Replace("&nbsp;", "") ?? "N/A";

                    jobList.Add(new JobListing
                    {
                        Date = date,
                        Title = title,
                        Url = jobUrl,
                        CompanyName = companyName
                    });
                }
            }
        }

        return jobList;
    }
}
