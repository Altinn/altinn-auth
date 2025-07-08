function buildAuthorizeBody(resourceId, action) {
    let body = {
        "Request": {
            "ReturnPolicyIdList": false,
            "AccessSubject": [
                {
                    "Attribute": [
                    ]
                }
            ],
            "Action": [
                {
                    "Attribute": [
                        {
                            "AttributeId": "urn:oasis:names:tc:xacml:1.0:action:action-id",
                            "Value": action,
                            "DataType": "http://www.w3.org/2001/XMLSchema#string"
                        }
                    ]
                }
            ],
            "Resource": [
                {
                    "Attribute": [
                        {
                            "AttributeId": "urn:altinn:resource",
                            "Value": resourceId
                        }
                    ]
                }
            ]
        }
    }
    return body;

}

export function buildClientDelegationAuthorizeBody(systemUserId, resourceId, customerOrgNo, action) {
    let body = buildAuthorizeBody(resourceId, action);
    body.Request.AccessSubject[0].Attribute.push(
        { 
            "AttributeId": "urn:altinn:systemuser:uuid",
            "Value": systemUserId
        });
    body.Request.Resource[0].Attribute.push(
        {
            "AttributeId": "urn:altinn:party:uuid",
            "Value": customerOrgNo,
            "DataType": "http://www.w3.org/2001/XMLSchema#string"
        });
    return body;
}

export function buildDaglAuthorizeBody(ssn, resourceId, orgno, action) {
    let body = buildAuthorizeBody(resourceId, action);
    body.Request.AccessSubject[0].Attribute.push(
        { 
            "AttributeId": "urn:altinn:person:identifier-no",
            "Value": ssn
        });
    body.Request.Resource[0].Attribute.push(
        {
            "AttributeId": "urn:altinn:organization:identifier-no",
            "Value": orgno,
            "DataType": "http://www.w3.org/2001/XMLSchema#string"
        });
    return body;
}

export function buildPrivAuthorizeBody(ssn, resourceId, action) {
    let body = buildAuthorizeBody(resourceId, action);
    body.Request.AccessSubject[0].Attribute.push(
        { 
            "AttributeId": "urn:altinn:person:identifier-no",
            "Value": ssn
        });
    body.Request.Resource[0].Attribute.push(
        {
            "AttributeId": "urn:altinn:person:identifier-no",
            "Value": ssn,
            "DataType": "http://www.w3.org/2001/XMLSchema#string"
        });
    return body;
}
