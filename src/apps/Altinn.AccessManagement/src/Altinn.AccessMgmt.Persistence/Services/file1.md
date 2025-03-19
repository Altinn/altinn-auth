# Klient delegering - Systembrukere

## Scenario #1

Det som må gjøres etter grunndata er på plass:
 
	- Finn eller opprett systembruker
	- Finn eller opprett tildeling fra Regnskapsfolka til Systembruker med rollen Agent - AgentAssignment
	- Finn tildelingen fra Bakeriet til Regnskapsfolka med rollen REGN - ClientAssignment
	- Opprett delegering mellom ClientAssignment og AgentAssignment - Delegation
	- Finn pakker tilgjengelig på ClientAssignment som skal delegeres - AssignmentPackage
	- Opprett Delegerings pakke oppføring med Delegation og Package
	- Resultat: Agent skal få opp Client i avgiverlisten
	- Resultat: Client skal få opp Agent i rettighetsholder listen

### Happy (Simple) Flow

Uten sjekker og valideringer så vil dette fungere. Om vi bare stoler på at data er riktig.

> /api/combined
```json
{
		"Client": "Bakeriet",
		"ClientRole": "REGN",
		"Facilitator": "Regnskapsfolk",
		"Agent": "SystemBruker-01",
		"AgentRole": "AGENT",
		"Package":"Regnskapsfører lønn",
		"User": "Viggo"
}
```

### Real flow

		- Data fra ER er importert
		- Det opprettes en systembruker POST /api/entity/ { id: uuid, name: something }
		- Det opprettes en tildeling (agentAssignment) av rollen Agent til systembruker for Regnskapsfolk POST /api/assignment/ { from: regnskapsfolk.Id, to: systembruker.id, role: agent }
		- Hent Assignments gitt til regnskapsfolk GET /api/assignment?to=regnskapsfolk
		- Finn en med rollen REGN fra firma Bakeriet, dette blir clientAssignment
		- Opprett en delegering mellom clientAssignment og agentAssignment POST /api/delegation { from: clientAssignment, to: agentAssignment }
		- Hent ut pakker tilgjengelig på clientAssignment som kan delegeres videre GET /api/assignment/{id}/packages
		- Velg pakke å delegere og legg den til på delegeringen POST api/delegation/{id}/packages { package: "..." }

## Data

### Tildeling

|Id|Fra|Rolle|Til|Kilde|Kommentar|
|-|-|-|-|-|-|
|A1|Bakeriet       |DAGL     |Gunnar         | ER    |-|
|A2|Bakeriet       |REGN     |Regnskapsfolk  | ER    |-|
|A3|Regnskapsfolk  |DAGL     |Fredrik        | ER    |-|
|A3|Regnskapsfolk  |TS       |Viggo          | A3    |Tilgangsstyrer|
|A3|Regnskapsfolk  |KA       |Viggo          | A3    |Klient Administrator|
|A4|Regnskapsfolk  |AGENT    |Stian          | A3    |-|
|A5|Regnskapsfolk  |AGENT    |SystemA        | REG   |-|

### Arvede tildelinger via RoleMap
|Fra|Rolle|Til|Kilde|Kommentar|
|-|-|-|-|-|
| Bakeriet       | `HA`     | Gunnar         | ER | Arvet fra DAGL     |
| Bakeriet       | `TS`     | Gunnar         | ER | Arvet fra DAGL     |
| Regnskapsfolk  | `HA`     | Fredrik        | ER | Arvet fra DAGL     |
| Regnskapsfolk  | `TS`     | Fredrik        | ER | Arvet fra DAGL     |

### Delegeringer

For å opprette en delegering må man være KundeAdmin hos fasilitator.

|Id|Fra|FraRolle|Fasilitator|TilRolle|Til|Utfører|
|-|-|-|-|-|-|-|
|D1|Bakeriet     | REGN   | Rengnskapsfolk   | AGENT  |  Stian  |  Fredrik  |


### TildelingsPakker

|TildelingsId|Pakke|
|-|-|
|A2|Regnskapsfører lønn|
|A2|Regnskapsfører med signeringsrett|
|A2|Regnskapsfører uten signeringsrett|


### DelegeringsPakker

|DelegeringsId|Pakke|
|-|-|
|D1|Regnskapsfører lønn|
