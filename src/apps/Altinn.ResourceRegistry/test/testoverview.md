# Testing Resource Registry

Resource Registry provides functionality to register resources with policies. Resource owners use these resources to authorize access to digital services hosted on other platforms. 

We have different approaches to test the various aspects of the Resource Registry.

## Wordlist

ORG - The organization owning the resources. It is responsible for defining the resource attributes and policy
policy. An authorization policy defines who can perform operations on the resource. This is done through rules in the policy.


## Requirements



### General

  - A Resource can only be published if all required data is complete 
  - A Resource needs a policy with a minimum of one valid rule

### Resource Attributes

- Title needs to be given in Norwegian Bokmål, Norwegian Nynorsk and English
- Description needs to be given in Norwegian Bokmål, Norwegian Nynorsk and English
- Rights description is required if users can delegate access to the resource. The description needs to be in Norwegian Bokmål, Norwegian Nynorsk, and English
- At least one contact information needs to be listed 
- ID is given only by lowercase a-z, 0-9 and - and _ 

### API

- To administrate access to resources through API the org needs access to scope 
- altinn:resourceregistry/resource.write for write access
- altinn:resourceregistry/resource.read for read access
- altinn:resourceregistry/resource.admin for admin
- A org can only administrate resource owned by themself. Ownership is defined by the HasCompetent authority
- It is not possible to change ID


### Altinn Studio Resource Admin

- It is possible to create a new resource from resource dasbhoard
- Users should be warned if there is missing information for the resource attributes

## User scenarios

### Create new resource with Policy from API

This scenario covers that flow where resource is created and posted to Resource Registry API

It checks 

- Validation
- Creation
- Usage

Valid result 

### Create new resource from Altinn Studio and publish



