namespace ScrapperHttpFunction.Helpers;

using CosmoDatabase.Entities;
using Models;

public class JobProcessing
{
    public static (List<JobInfo> toAdd, List<JobInfo> toRemove) ProcessJobListings(List<JobListing> income, List<JobInfo> exist)
    {
        var toAdd = income
            .Where(x => exist.All(e => e.Hash != x.Hash))
            .Select(j => 
                new JobInfo
                {
                    Id = Ulid.NewUlid().ToString(),
                    Date = j.Date,
                    Title = j.Title,
                    Url = j.Url,
                    CompanyName = j.CompanyName
                })
            .ToList();

        var toRemove = exist.Where(x => income.All(e => e.Hash != x.Hash)).ToList();
        
        return (toAdd, toRemove);
    }
}