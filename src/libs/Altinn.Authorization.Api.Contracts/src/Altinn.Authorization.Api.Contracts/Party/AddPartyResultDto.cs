namespace Altinn.Authorization.Api.Contracts.Party
{
    public class AddPartyResultDto
    {
        public Guid PartyUuid { get; set; }

        public bool PartyCreated { get; set; }
    }
}
