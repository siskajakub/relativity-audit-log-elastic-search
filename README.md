# relativity-audit-log-elastic-search
Relativity Application to synchronize Audit Log to Elastic Search

# Install
## 1) Create Instance Settings
Create required Relativity Instance Settings entries:  
Name | Section | Value Type | Value (example) | Description
---- | ------- | ---------- | --------------- | -----------
ElasticSearchUris | Relativity.AuditLogElasticSearch | Text | xxxxxxxxx | URIs of the ES cluster nodes separated by a semicolon.
ElasticSearchApiKey | Relativity.AuditLogElasticSearch | Text | xxxx:xxxx | ES cluster authentication API Key in `id:key` format (can be empty).
ElasticSearchIndexPrefix | Relativity.AuditLogElasticSearch | Text | relativity- | Prefix of the ES indices.
ElasticSearchIndexReplicas | Relativity.AuditLogElasticSearch | Integer 32-bit | 1 | Number of replicas for newly created ES indices.
ElasticSearchIndexShards | Relativity.AuditLogElasticSearch | Integer 32-bit | 2 | Number of shards for newly created ES indices.
ElasticSearchSyncSize | Relativity.AuditLogElasticSearch | Integer 32-bit | 1000000 | Positive integer, ideally multiplier of 1000.

## 2) Compile DLL
Download the source code and compile the code using Microsoft Visual Studio 2019.  
For more details on how to setup your development environemnt, please follow official [Relativity documentation](https://platform.relativity.com/10.3/index.htm#Relativity_Platform/Setting_up_your_development_environment.htm).

## 3) Create Relativity Application
Create a custom Relativity application and upload all required resource files.  
For more details on how to setup your own application, please follow official [Relativity applications documentation](https://platform.relativity.com/10.3/index.htm#Building_Relativity_applications/Relativity_applications.htm).  
You can also use pre-assembled `.rap` file and load it directly as a new Relativity Application.

## 4) Create Reltivity Agent
After succesfull instalation of the Relativity Application, Audit Log Elastic Search Agent needs to be deployed.  
Agent is responsible for synchronizing data to ES.  
Multiple agants can be deployed. Each run, agent synchronizes up to ElasticSearchSyncSize Audit Log entries.

## 5) Firewall
If applicable, create firewall rule(s) allowing communication on port 9200 between ES and servers running Relativity Agent(s).

# Update
If you are updating Audit Log to Elastic Search from previous version, please re-create Audit Log Elastic Search Agent.

# Synchronization with ES
When application is installed into the particular workspace, workspace's Audit Log is automatically being synchronised to ES.  
When application is unistalled or workspace is deleted. Associated ES index is deleted automatically as well.

# Notes
Relativity Audit Log Elastic Search application support Relativity 10.0.318.5 and later.
Relativity Audit Log Elastic Search application support Elastic Search 7.12.0 and later.
Application was developed and tested in Relativity 10.3.
