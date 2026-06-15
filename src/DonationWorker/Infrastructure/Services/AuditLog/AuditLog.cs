using Amazon.DynamoDBv2.DataModel;

namespace DonationWorker.Infrastructure.Services.AuditLog
{
    [DynamoDBTable("cs-audit-log")]
    public class AuditLog
    {
        [DynamoDBHashKey] 
        public string PK { get; set; } 
        [DynamoDBRangeKey] 
        public string SK { get; set; }
        [DynamoDBProperty("ResourceId")]
        public string ResourceId { get; set; }
        public string ServiceName { get; set; }
        public string Operation { get; set; }
        public string ChangedBy { get; set; }
        public string Payload { get; set; }
        public string IpAddress { get; set; }
        [DynamoDBProperty("TTL")]
        public long ExpirationTime { get; set; } // Unix Timestamp para autodelete
    }

    /*
        Atributo	        Exemplo (Usuário)	                            Exemplo (Pagamento)
        PK	                ENTITY#USER#guid-123	                        ENTITY#PAYMENT#pay-456
        SK	                TS#2026-04-05T10:00:00Z	                        TS#2026-04-05T10:05:00Z
        Service	            Users-API	                                    Payments-API
        Action	            UpdateStatus	                                AuthorizeCapture
        Data	            { "old": "Active", "new": "Suspended" }	        { "amount": 150.00, "status": "Paid" }
        User	            admin@fiap.com	                                system-gateway
    */
}
