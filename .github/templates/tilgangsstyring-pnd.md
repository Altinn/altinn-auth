---
title: Deploy & Patching W{{ date | date('ww') }}
labels: 'kind/deploy_patch, team/tilgangsstyring'
---
## Tuesday: Prod-deploy

- [ ] Deploy updates to APIM production (if updated since last deploy)
  - [ ] Check [APIM deploys to TT02](https://dev.azure.com/brreg/altinn-studio-ops/_build?definitionId=125) for updates to `#authorization` and `#access-management`. If none of them have been deployed the last week, no production APIM deploy is needed.
  - [ ] Deploy products with changes [to APIM production](https://dev.azure.com/brreg/altinn-studio-ops/_build?definitionId=128).
- [ ] Deploy[^1] all platform components to production[^2].
  - [ ] Deploy [Authorization] to production.
  - [ ] Deploy [Delegation Events] to production.
  - [ ] Deploy [Access Management] to production.
  - [ ] Verify [cluster 01 new pods](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/1ab2d164-1861-4ff8-be8c-069c3ee3b70a/resourceGroups/altinnplatform-prod-rg/providers/Microsoft.ContainerService/managedClusters/platform-prod-01-aks/workloads) or [cluster 02 new pods](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/1ab2d164-1861-4ff8-be8c-069c3ee3b70a/resourceGroups/altinnplatform-prod-rg/providers/Microsoft.ContainerService/managedClusters/platform-prod-02-aks/workloads) startup without errors.
  - [ ] Verify [function app deployment](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/1ab2d164-1861-4ff8-be8c-069c3ee3b70a/resourceGroups/altinnplatform-prod-rg/providers/Microsoft.Web/sites/altinn-prod-delegation-func/appServices).
  - [ ] Grab a coffee.
  - [ ] Verify no errors reported for the components in Slack channels [#alerts-prod](https://altinndevops.slack.com/archives/C014H7WPSUB) or [#alerts-prod-critical](https://altinndevops.slack.com/archives/C012108PYBV).
  - [ ] Post-deploy [Authorization] and [Access Management] if everything looks good.
  - [ ] Post-deploy [Delegation Events] if everything looks good.
- [ ] Deploy frontend to production (if a new release was made and deployed to TT02 last week)
  - [ ] Check [Access Management Frontend Releases](https://github.com/Altinn/altinn-access-management-frontend/releases) if a new release was made last week and [deployed to TT02](https://github.com/Altinn/altinn-access-management-frontend/deployments/TT02)
  - [ ] Deploy new release of [Access Management Frontend] to production.
  - [ ] Verify [new revision of container app](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/1ab2d164-1861-4ff8-be8c-069c3ee3b70a/resourceGroups/accessmanagementui-prod-rg/providers/Microsoft.App/containerApps/altinn-prod-amui-app/revisionManagement) started without error.
- [ ] If any of the above steps or links needs correction [update the automatic issue template](https://github.com/Altinn/altinn-authorization/blob/main/.github/templates/tilgangsstyring-pnd.md).

## Wednesday: TT02-deploy

- [ ] Deploy updates to APIM TT02 (if updated since last deploy)
  - [ ] Check [APIM deploys to AT](https://dev.azure.com/brreg/altinn-studio-ops/_build?definitionId=124) for updates to `#authorization` and `#access-management`. If none of them have been deployed the last week, no TT02 APIM deploy is needed.
  - [ ] Deploy products with changes [to TT02 APIM](https://dev.azure.com/brreg/altinn-studio-ops/_build?definitionId=125).
- [ ] Deploy[^1] all platform components to TT02[^3].
  - [ ] Deploy [Authorization] to TT02.
  - [ ] Deploy [Delegation Events] to TT02.
  - [ ] Deploy [Access Management] to TT02.
  - [ ] Verify [cluster 01 new pods](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/dd6d3e08-a70f-4f71-8847-781ddc5d8468/resourceGroups/altinnplatform-tt02-rg/providers/Microsoft.ContainerService/managedClusters/platform-tt02-01-aks/workloads) or [cluster 02 new pods](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/dd6d3e08-a70f-4f71-8847-781ddc5d8468/resourceGroups/altinnplatform-tt02-rg/providers/Microsoft.ContainerService/managedClusters/platform-tt02-02-aks/workloads) startup without errors.
  - [ ] Verify [function app deployment](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/dd6d3e08-a70f-4f71-8847-781ddc5d8468/resourceGroups/altinnplatform-tt02-rg/providers/Microsoft.Web/sites/altinn-tt02-delegation-func/appServices).
  - [ ] Grab a coffee.
  - [ ] Verify [Authorization K6 automated test](https://dev.azure.com/brreg/altinn-studio/_build?definitionId=414) results.
  - [ ] Verify [Authorization Bruno automated test](https://dev.azure.com/brreg/altinn-studio/_build?definitionId=480) results.
  - [ ] Verify [Access Management K6 automated test](https://dev.azure.com/brreg/altinn-studio/_build?definitionId=412) results.
  - [ ] Verify [Access Management Bruno automated test](https://dev.azure.com/brreg/altinn-studio/_build?definitionId=475) results.
  - [ ] Verify no errors reported for the components in Slack channels [#alerts-prod](https://altinndevops.slack.com/archives/C014H7WPSUB) or [#alerts-prod-critical](https://altinndevops.slack.com/archives/C012108PYBV) (Yes, TT02 alerts also goes here).
  - [ ] Post-deploy [Authorization] and [Access Management] if everything looks good.
  - [ ] Post-deploy [Delegation Events] if everything looks good.
- [ ] Deploy frontend to TT02 (if a draft release with changes is waiting for deploy)
  - [ ] Check [Access Management Frontend Releases](https://github.com/Altinn/altinn-access-management-frontend/releases) if a release draft is waiting.
  - [ ] Publish the draft as the next release as `v{year}.{releaseNumber}`
  - [ ] Deploy new release of [Access Management Frontend] to TT02.
  - [ ] Verify [new revision of container app](https://portal.azure.com/#@ai-dev.no/resource/subscriptions/dd6d3e08-a70f-4f71-8847-781ddc5d8468/resourceGroups/accessmanagementui-tt02-rg/providers/Microsoft.App/containerApps/altinn-tt02-amui-app/revisionManagement) started without error.
  - [ ] Verify [Cypress automated tests](https://github.com/Altinn/altinn-access-management-frontend/actions/workflows/cypress.yml) results.
- [ ] If any of the above steps or links needs correction [update the automatic issue template](https://github.com/Altinn/altinn-authorization/blob/main/.github/templates/tilgangsstyring-pnd.md).

## Thursday: Patching

### Go through all github pull-requests from the dependency bots in all repos

- [ ] [Authorization](https://github.com/Altinn/altinn-authorization/pulls)
- [ ] [Access Management](https://github.com/Altinn/altinn-access-management/pulls)
- [ ] [Access Management Frontend](https://github.com/Altinn/altinn-access-managment-frontend/pulls)

---

## Deployrutine
Det som skal deployes til PROD skal ha vært ute i TT02 i en uke. Før man deployer må man sørge for at man ikke introduserer nye feil, så da må man sjekke et par steder:

### Sjekk Slack-kanalene

![slack-alerts](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/slack-alerts.png)

For deploy til PROD må du gå gjennom kanalene "alerts-prod" og "alerts-prod-critical" for å se om det er noen feil som berører eller er forårsaket av repoet som skal deployes. For TT02 sjekkes "alerts-test". Hvis det ser greit ut her kan du gå videre og sjekke ut https://portal.azure.com.

### Sjekk failures i portal.azure.com
Gå til https://portal.azure.com -> Application Insights -> tt02-platform-ai/prod-platform-ai -> failures. 

![failures-in-azure](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/failures-in-azure.png)

Klikk på *Roles*, så *Clear selection*, og velg rollen som tilsvarer komponenten du skal deploye. Hvis det er feil her, skriv på Slack (team-autorisasjon eller utviklere-autorisasjon) og hør om det er noe de kjenner til, og om det er en stopper for deploy. Hvis svaret er at det er OK, eller om du ikke finner noen feil, gå videre til neste steg.

### Pre-deploy approval
Når alt er klart går du til https://dev.azure.com/brreg/altinn-studio. Klikk deg inn på Pipelines -> Releases -> komponenten som skal deployes (Access Management i dette tilfellet) -> Production (på tirsdager, ellers TT02 på onsdager). Klikk på den nyligste blå knappen.

![pipeline-releases](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/pipeline-releases.png)

Når du klikker på *Approve* starter deploy.

![pre-deployment](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/pre-deployment.png)

### Post-deploy approval
Deploy tar noen minutter. Når den er ferdig må man Approve Post-Deployment. Før du gjør dette må du sjekke et par ting.

### Sjekk pods i portal.azure.com
Gå til https://portal.azure.com/  og velg Kubernetes services.

![pods](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/pods.png)

Klikk på platform-prod-02-aks (eller tt02 hvis deployet til TT02) -> Workloads -> Pods. Her kan du sortere på *Age* så alle de nyeste pod'ene kommer først. I dette skjermbildet ble Access Management deployet, man ser at alle fire Access Management pod'ene har en grønn hake under Ready og *Running* som status. Hvis noen av pod'ene ikke er Ready, kan det hende du må vente noen minutter og sjekke igjen.

#### Unntak for Delegation Events: sjekk functions i stedet for pods
TODO

### Post-deploy Approval
Hvis alt ser bra ut kan du gå tilbake til https://dev.azure.com/brreg/altinn-studio og finne Releasen som som ble deployet og approve post-deployment.

![post-deployment](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/post-deployment.png)

![approved](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/approved.png)

[Authorization]: https://dev.azure.com/brreg/altinn-studio/_release?_a=releases&view=mine&definitionId=23
[Delegation Events]: https://dev.azure.com/brreg/altinn-studio/_release?_a=releases&view=mine&definitionId=33
[Access Management]: https://dev.azure.com/brreg/altinn-studio/_release?_a=releases&view=mine&definitionId=37
[Access Management Frontend]: https://github.com/Altinn/altinn-access-management-frontend/actions/workflows/deploy-to-environment.yml

[^1]: Approve pending prod releases by clicking the blue production chip and clicking approve. ![image-20240306133832594](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/ado-pending-approval-screen.png)
[^2]: Look for any blue production chips. ![image-20240306133137061](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/ado-prod-button.png)
[^3]: Look for any blue TT02 chips. ![tt02-button](https://raw.githubusercontent.com/Altinn/altinn-authorization/main/.github/images/ado-tt02-button.png)