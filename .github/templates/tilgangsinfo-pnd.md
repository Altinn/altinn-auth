---
title: Deploy & Patching W{{ date | date('ww') }}
labels: "kind/deploy_patch, team/tilgangsinfo"
---

## Tuesday

- [ ] Deploy[^1] all components to production[^2].
  - [ ] Deploy [Authentication] to production.
  - [ ] Deploy [Resource Registry] to production.
  - [ ] Deploy [Register] to production.
  - [ ] Deploy [Auditlog] to production[^4].
- [ ] Deploy updates to APIM (if updated since last deploy)
  - [ ] Check [apim deploys to tt](https://dev.azure.com/brreg/altinn-studio-ops/_build?definitionId=125) for updates to `#resourceregistry`, `#authentication`, and `#register`. If none of them have been deployed the last week, no change needed.
  - [ ] Run [apim prod pipeline](https://dev.azure.com/brreg/altinn-studio-ops/_build?definitionId=128)[^8] and select the product that needs to be updated.
- [ ] Go grab a coffee
- [ ] Post-deploy[^3] all components in production.
  - [ ] Post-deploy [Authentication] after checking [aks workloads](https://portal.azure.com/#view/Microsoft_Azure_ContainerService/AksK8ResourceMenuBlade/~/overview-DaemonSet/aksClusterId/%2Fsubscriptions%2F1ab2d164-1861-4ff8-be8c-069c3ee3b70a%2FresourceGroups%2Faltinnplatform-prod-rg%2Fproviders%2FMicrosoft.ContainerService%2FmanagedClusters%2Fplatform-prod-02-aks/resource~/%7B%22kind%22%3A%22DaemonSet%22%2C%22metadata%22%3A%7B%22name%22%3A%22altinn-authentication%22%2C%22namespace%22%3A%22default%22%2C%22uid%22%3A%22c3655f5b-23c8-4901-82ff-e5aeb2515e95%22%7D%2C%22spec%22%3A%7B%22selector%22%3A%7B%22matchLabels%22%3A%7B%22app%22%3A%22altinn-authentication%22%2C%22release%22%3A%22altinn-authentication%22%7D%7D%7D%7D).
  - [ ] Post-deploy [Resource Registry] after checking [aks workloads](https://portal.azure.com/#view/Microsoft_Azure_ContainerService/AksK8ResourceMenuBlade/~/overview-DaemonSet/aksClusterId/%2Fsubscriptions%2F1ab2d164-1861-4ff8-be8c-069c3ee3b70a%2FresourceGroups%2Faltinnplatform-prod-rg%2Fproviders%2FMicrosoft.ContainerService%2FmanagedClusters%2Fplatform-prod-02-aks/resource~/%7B%22kind%22%3A%22DaemonSet%22%2C%22metadata%22%3A%7B%22name%22%3A%22altinn-resource-registry%22%2C%22namespace%22%3A%22default%22%2C%22uid%22%3A%22d60636dc-371b-4bbb-b170-905410c90d9d%22%7D%2C%22spec%22%3A%7B%22selector%22%3A%7B%22matchLabels%22%3A%7B%22app%22%3A%22altinn-resource-registry%22%2C%22release%22%3A%22altinn-resource-registry%22%7D%7D%7D%7D).
  - [ ] Post-deploy [Register] after checking [aks workloads](https://portal.azure.com/#view/Microsoft_Azure_ContainerService/AksK8ResourceMenuBlade/~/overview-DaemonSet/aksClusterId/%2Fsubscriptions%2F1ab2d164-1861-4ff8-be8c-069c3ee3b70a%2FresourceGroups%2Faltinnplatform-prod-rg%2Fproviders%2FMicrosoft.ContainerService%2FmanagedClusters%2Fplatform-prod-02-aks/resource~/%7B%22kind%22%3A%22DaemonSet%22%2C%22metadata%22%3A%7B%22name%22%3A%22altinn-register%22%2C%22namespace%22%3A%22default%22%2C%22uid%22%3A%22fd78c3fb-8b06-4a42-8b35-79c1b243e107%22%7D%2C%22spec%22%3A%7B%22selector%22%3A%7B%22matchLabels%22%3A%7B%22app%22%3A%22altinn-register%22%2C%22release%22%3A%22altinn-register%22%7D%7D%7D%7D).
  - [ ] Post-deploy of [auditlog](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/1ab2d164-1861-4ff8-be8c-069c3ee3b70a/resourceGroups/auditlog-prod-rg/overview), verify [containerapp](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/1ab2d164-1861-4ff8-be8c-069c3ee3b70a/resourceGroups/auditlog-prod-rg/providers/Microsoft.App/containerApps/altinn-prod-auditlog-app/containerapp)[^6] and [functionapp](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/1ab2d164-1861-4ff8-be8c-069c3ee3b70a/resourceGroups/auditlog-prod-rg/providers/Microsoft.Web/sites/altinn-prod-auditlog-fa/appServices)[^7].

## Wednesday

- [ ] Deploy[^1] all components to TT02.
  - [ ] Deploy [Authentication] to TT02.
  - [ ] Deploy [Resource Registry] to TT02.
  - [ ] Deploy [Register] to TT02.
  - [ ] Deploy [Auditlog][Auditlog Releases] to TT02[^5].
- [ ] Deploy updates to APIM (if updated since last deploy)
  - [ ] Check [apim deploys to at](https://dev.azure.com/brreg/altinn-studio-ops/_build?definitionId=124) for updates to `#resourceregistry`, `#authentication`, and `#register`. If none of them have been deployed the last week, no change needed.
  - [ ] Run [apim tt02 pipeline](https://dev.azure.com/brreg/altinn-studio-ops/_build?definitionId=125)[^8] and select the product that needs to be updated.
- [ ] Go grab a coffee
- [ ] Post-deploy[^3] all components in TT02.
  - [ ] Post-deploy [Authentication] after checking [aks workloads](https://portal.azure.com/#view/Microsoft_Azure_ContainerService/AksK8ResourceMenuBlade/~/overview-DaemonSet/aksClusterId/%2Fsubscriptions%2Fdd6d3e08-a70f-4f71-8847-781ddc5d8468%2FresourceGroups%2Faltinnplatform-tt02-rg%2Fproviders%2FMicrosoft.ContainerService%2FmanagedClusters%2Fplatform-tt02-02-aks/resource~/%7B%22kind%22%3A%22DaemonSet%22%2C%22metadata%22%3A%7B%22name%22%3A%22altinn-authentication%22%2C%22namespace%22%3A%22default%22%2C%22uid%22%3A%2230435626-9bfa-4c59-8982-2c67f5e12236%22%7D%2C%22spec%22%3A%7B%22selector%22%3A%7B%22matchLabels%22%3A%7B%22app%22%3A%22altinn-authentication%22%2C%22release%22%3A%22altinn-authentication%22%7D%7D%7D%7D).
  - [ ] Post-deploy [Resource Registry] after checking [aks workloads](https://portal.azure.com/#view/Microsoft_Azure_ContainerService/AksK8ResourceMenuBlade/~/overview-DaemonSet/aksClusterId/%2Fsubscriptions%2Fdd6d3e08-a70f-4f71-8847-781ddc5d8468%2FresourceGroups%2Faltinnplatform-tt02-rg%2Fproviders%2FMicrosoft.ContainerService%2FmanagedClusters%2Fplatform-tt02-02-aks/resource~/%7B%22kind%22%3A%22DaemonSet%22%2C%22metadata%22%3A%7B%22name%22%3A%22altinn-resource-registry%22%2C%22namespace%22%3A%22default%22%2C%22uid%22%3A%228a2dca87-471b-47a8-899c-817b6fd7ea70%22%7D%2C%22spec%22%3A%7B%22selector%22%3A%7B%22matchLabels%22%3A%7B%22app%22%3A%22altinn-resource-registry%22%2C%22release%22%3A%22altinn-resource-registry%22%7D%7D%7D%7D).
  - [ ] Post-deploy [Register] after checking [aks workloads](https://portal.azure.com/#view/Microsoft_Azure_ContainerService/AksK8ResourceMenuBlade/~/overview-DaemonSet/aksClusterId/%2Fsubscriptions%2Fdd6d3e08-a70f-4f71-8847-781ddc5d8468%2FresourceGroups%2Faltinnplatform-tt02-rg%2Fproviders%2FMicrosoft.ContainerService%2FmanagedClusters%2Fplatform-tt02-02-aks/resource~/%7B%22kind%22%3A%22DaemonSet%22%2C%22metadata%22%3A%7B%22name%22%3A%22altinn-register%22%2C%22namespace%22%3A%22default%22%2C%22uid%22%3A%22ed2e3206-ffd2-49af-8e41-1e607bea6471%22%7D%2C%22spec%22%3A%7B%22selector%22%3A%7B%22matchLabels%22%3A%7B%22app%22%3A%22altinn-register%22%2C%22release%22%3A%22altinn-register%22%7D%7D%7D%7D).
  - [ ] Post-deploy of [auditlog](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/dd6d3e08-a70f-4f71-8847-781ddc5d8468/resourceGroups/auditlog-tt02-rg/overview), verify [containerapp](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/dd6d3e08-a70f-4f71-8847-781ddc5d8468/resourceGroups/auditlog-tt02-rg/providers/Microsoft.App/containerApps/altinn-tt02-auditlog-app/containerapp)[^6] and [functionapp](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/dd6d3e08-a70f-4f71-8847-781ddc5d8468/resourceGroups/auditlog-tt02-rg/providers/Microsoft.Web/sites/altinn-tt02-auditlog-fa/appServices)[^7].

## Thursday

#### Go through all github pull-requests from the dependency bots in all repos.

- [ ] [Authentication](https://github.com/Altinn/altinn-authentication).
- [ ] [Authentication UI](https://github.com/Altinn/altinn-authentication-frontend).
- [ ] [Resource Registry](https://github.com/Altinn/altinn-resource-registry).
- [ ] [Register](https://github.com/Altinn/altinn-register).
- [ ] [Auditlog](https://github.com/Altinn/altinn-auth-audit-log).
- [ ] [Authorization Utils](https://github.com/Altinn/altinn-authorization-utils).

[Authentication]: https://dev.azure.com/brreg/altinn-studio/_release?_a=releases&view=all&definitionId=20
[Resource Registry]: https://dev.azure.com/brreg/altinn-studio/_release?_a=releases&view=all&definitionId=36
[Register]: https://dev.azure.com/brreg/altinn-studio/_release?_a=releases&view=all&definitionId=19
[Auditlog]: https://github.com/Altinn/altinn-auth-audit-log/actions/workflows/deploy-after-release.yml
[Auditlog Releases]: https://github.com/Altinn/altinn-auth-audit-log/releases

[^1]: Approve pending prod releases by clicking the blue production chip and clicking approve. ![image-20240306133832594](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/ado-pending-approval-screen.png)
[^2]: Look for any blue production chips. ![image-20240306133137061](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/ado-prod-button.png)
[^3]: This is done after checking that everything is running as it should.
[^4]:
    Check if there are any awaiting deployments to production for [Auditlog]  
    ![Awaiting deployment of Prod](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/awaiting-deploy.png)  
    Click on the awaiting deployment. You can see that a deployment to TT02 was successfull last week. Click on Review deployments  
    ![Detailed view of Awaiting deployment of prod](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/awaiting-deploy-prod-detail.png)  
    Click on the prod checkbox and then click approve and deploy. This will trigger a deployment to production.  
    ![Review deployment of prod](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/review-deploy-prod.png)

[^5]:
    The new components are deployed via github actions. For auditlog, a release is scheduled every wednesday. Go to [Auditlog Releases] and check if there are any draft release is found.  
    ![Releases list](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/release-draft.png)  
    click on Edit Draft and set the release title as the tag version f.eks in the picture you can see that the tag is 2024.4.3 and the release title therefore should be set as 2024.4.3.  
    ![Edit release notes draft](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/edit-draft-releasenotes.png)  
    Scroll down and you will find the button "Publish Release"  
    ![Publish release](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/publishrelease.png)  
    once the publish release is clicked, release notes will be published and a deploy will be triggered to TT02.  
    ![Published release](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/publishedrelease.png)  
    You can see that the deploy is triggered and the approvers are notified about the awaiting deployment of [Auditlog]  
    ![Awaiting deployment of TT02](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/awaiting-deploy-tt02.png)  
    Click on the awaiting deployment  
    ![Detailed view of Awaiting deployment of TT02](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/awaiting-deploy-tt02-detail.png)  
    Click on review deployments. Click the tt02 checkbox and click approve and deploy button  
    ![Review of TT02 deployment](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/review-deploy-tt02.png)  
    Now you can see that a deployment is triggered to TT02  
    ![deploy progress of TT02](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/deploy-progress-tt02.png)  
    Once the package is successfully deployed to TT02, you can see that a deployment to production is triggered. This will be approved by the deployer on the following Tuesday  
    ![Image showing tt02 deployed and producntion deployment is triggered](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/deploy-prod-triggered.png)

[^6]:
    Verify that the newly deployed package has the right image tag ![Properties of container app](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/containerapp-props.png)  
    Check the provisioning status in the container apps overview page  
    ![provision status of container app](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/containerapp-provision-status.png)

[^7]:
    Verify the function app deployment status in the deployment logs in deployment center of the function app  
    ![deployment status of function app](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/functionapp-deploy-status.png)

[^8]: ![update apim prod](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/update-apim-prod.png)
