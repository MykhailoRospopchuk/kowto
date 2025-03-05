# kowto

## Overview
The ```kowto``` is a C# project designed to work with Azure Functions. It includes functionality for interacting with Azure Cosmos DB and Logic Apps.

## Idea
In this project at this stage, there is one simple function - to notify me when a new vacancy appears on the DOU website. Once an hour between 6:00 and 22:00 (UTC) the function makes a query on DOU with the specified filter and loads information about vacancies. Compares with the values ​​in the database and if there are new vacancies, it calls a logic application that sends an email with information about the new vacancy

![second](images/second.png)

## Key Features
- Azure Functions Integration: The project is built using Azure Functions, making it scalable and efficient for serverless applications.
- Cosmos DB Integration: It includes a Cosmos DB container and wrapper for database operations.
- Logic App Workflow: The project interacts with Azure Logic Apps through a specified workflow URL.
- Application Insights: Integration with Application Insights for monitoring and diagnostics.
## Setup Instructions
1. **Clone the repository:**

```bash
git clone https://github.com/MykhailoRospopchuk/kowto.git
```

2. **Navigate to the project directory and configure the environment variables:**

- LogicAppWorkflowURL: URL for the Logic App Workflow.
- CosmoConnectionString: Connection string for Cosmos DB.
- ```local.settings.json``` should contains this variables
```json
{
  "IsEncrypted": false,
  "Values": {
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "",
    "AzureWebJobsStorage": "",
    "DEPLOYMENT_STORAGE_CONNECTION_STRING": "",
    "LogicAppWorkflowURL": "",
    "CommunicationLogicApp":"",
    "CosmoConnectionString": "",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

3. **As you already understand you need set up your NO-SQL database (CosmoDB in this our case) and create simple Azure Logic App**

As I have already discovered "through scientific trial and error," if you use Outlook mail, it can be temporarily blocked if you send a message once an hour.

![first](images/first.png)

So I tried another option using Azure Communication Service with Email Communication Service. You can create default azure managed domain ```DoNotReplay@************azurecomm.net``` (and as expect it will go in spam folder on mailbox). But it works
![third](images/third.png)

4. **Build and run the project:**

```bash
func start
```
## Usage
The project provides Azure Functions that interact with Cosmos DB and Logic Apps. Ensure that the necessary environment variables are set before running the application.

Contribution Guidelines
Contributions are welcome! Please fork the repository and submit pull requests for any enhancements or bug fixes.

Feel free to adjust the review based on any additional details or specific features you want to highlight in your project.