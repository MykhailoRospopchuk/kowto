namespace ScrapperHttpFunction.Helpers;

using CosmoDatabase.Entities;
using Models;

public class JobProcessingHelper
{
    public static (List<JobInfo> toAdd, List<JobInfo> toRemove) ProcessFullJobListings(List<JobListing> income, List<JobInfo> exist)
    {
        var toAdd = ToAdd(income, exist);

        var toRemove = ToRemove(income, exist);
        
        return (toAdd, toRemove);
    }

    public static List<JobInfo> ToAdd(List<JobListing> income, List<JobInfo> exist)
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
                    CompanyName = j.CompanyName,
                    Hash = j.Hash
                })
            .ToList();
        
        return toAdd;
    }

    public static List<JobInfo> ToRemove(List<JobListing> income, List<JobInfo> exist)
    {
        var toRemove = exist.Where(x => income.All(e => e.Hash != x.Hash)).ToList();
        return toRemove;
    }
}