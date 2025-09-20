---
title: Azure DevOps JSON-Patch payloads
---

# Azure DevOps JSON-Patch examples

This page contains example request payloads used by the AzDO writer in `atools/add_issue.py` and sample failure responses you may encounter.

## Create work item (JSON-Patch) example

Request (PATCH array) to create a work item via the AzDO REST API (POST to `https://dev.azure.com/{org}/{project}/_apis/wit/workitems/${type}?api-version=7.0`):

```json
[
  { "op": "add", "path": "/fields/System.Title", "value": "Example issue title" },
  { "op": "add", "path": "/fields/System.Description", "value": "Full description with markdown or HTML" },
  { "op": "add", "path": "/fields/Microsoft.VSTS.Common.AcceptanceCriteria", "value": "- AC1\n- AC2" },
  { "op": "add", "path": "/relations/-", "value": { "rel": "System.LinkTypes.Hierarchy-Reverse", "url": "https://dev.azure.com/.../workitems/123", "attributes": { "comment": "Related work" } } }
]
```

Notes:
- The `AcceptanceCriteria` field is optional and will only be included if the project defines a field with that reference name. The writer attempts to discover field reference names via the AzDO work item type schema.

## Example success response (201 Created)

```json
{
  "id": 456,
  "rev": 1,
  "fields": {
    "System.Title": "Example issue title",
    "System.State": "New",
    "System.WorkItemType": "Task"
  },
  "url": "https://dev.azure.com/myorg/_apis/wit/workItems/456"
}
```

## Example failure responses

- 401 Unauthorized (invalid/missing PAT):

```json
{
  "message": "TF400813: Resource not available for anonymous access. Client authentication required."
}
```

- 400 Bad Request (invalid field or missing required fields):

```json
{
  "errorCode": "InvalidFieldValue",
  "message": "The field 'Microsoft.VSTS.Common.AcceptanceCriteria' is not valid for work item type 'Task'."
}
```

- 404 Not Found (project or area path missing):

```json
{
  "message": "TF400898: The project 'MyProject' does not exist or you do not have permission to access it."
}
```

## Troubleshooting

- Ensure PAT has 'Work Items (read and write)' scope and the token is provided via `AZURE_DEVOPS_EXT_PAT` or `AZURE_DEVOPS_PAT` environment variable.
- Ensure `AZURE_DEVOPS_ORG` and project name are correct.
