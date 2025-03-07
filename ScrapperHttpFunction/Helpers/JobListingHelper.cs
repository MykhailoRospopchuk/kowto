namespace ScrapperHttpFunction.Helpers;

using HtmlAgilityPack;
using Models;
using Enums;

public class JobListingHelper
{
    public static List<JobListing> FetchJobListings(PathEnum path, string rawPage)
    {
        switch (path)
        {
            case PathEnum.DOU:
                return FetchJobDouListings(rawPage);
            case PathEnum.Djinni:
                return FetchJobDjinniListings(rawPage);
            default:
                throw new Exception($"Unknown path: {path}");
        }
    }

    private static List<JobListing> FetchJobDouListings(string rawPage)
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
                        CompanyName = companyName,
                        Hash = HashHelper.GetHashMd5(new []{date, title, jobUrl, companyName})
                    });
                }
            }
        }

        return jobList;
    }

    private static List<JobListing> FetchJobDjinniListings(string rawPage)
    {
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(rawPage);

        List<JobListing> jobList = new List<JobListing>();

        // Find the main vacancy container
        var mainContainer = doc.DocumentNode.SelectSingleNode("//main[@id='jobs_main']");

        if (mainContainer is null)
        {
            throw new NullReferenceException("Could not find main container");
        }

        var vacancyList = mainContainer.SelectSingleNode(".//ul[@class='list-unstyled list-jobs mb-4']");
        if (vacancyList != null)
        {
            var jobNodes = vacancyList.SelectNodes(".//li[contains(@id, 'job-item')]");

            if (jobNodes != null)
            {
                foreach (var job in jobNodes)
                {
                    var vacancyTitleNode = job.SelectSingleNode(".//a[@class='job-item__title-link']");
                    string title = vacancyTitleNode?.InnerText.Trim() ?? "N/A";
                    string jobUrl = vacancyTitleNode?.GetAttributeValue("href", "#") ?? String.Empty;

                    var companyCommonNode = job.SelectSingleNode(".//div[contains(@class, 'd-inline-flex align-items-center gap-1')]");
                    string companyName;
                    var companyNode = companyCommonNode.SelectSingleNode(".//a[contains(@class, 'text-body js-analytics-event')]");

                    if (companyNode != null)
                    {
                        companyName = companyNode.InnerText.Trim();
                    }
                    else
                    {
                        companyNode = companyCommonNode.SelectSingleNode(".//span[contains(@class, 'text-nowrap')]");
                        companyName = companyNode?.InnerText.Trim() ?? "Not Found";
                    }

                    // TODO: I need to come up with something because this element is probably created by a script.
                    var dateNode = job.SelectSingleNode(".//span[@data-original-title]");
                    string date = dateNode?.GetAttributeValue("data-original-title", "Not Found") ?? "Not Found";

                    jobList.Add(new JobListing
                    {
                        Date = date,
                        Title = title,
                        Url = string.IsNullOrEmpty(jobUrl) ? "#" : "https://djinni.co" + jobUrl,
                        CompanyName = companyName,
                        Hash = HashHelper.GetHashMd5(new []{date, title, jobUrl, companyName})
                    });
                }
            }
        }

        return jobList;
    }
}
