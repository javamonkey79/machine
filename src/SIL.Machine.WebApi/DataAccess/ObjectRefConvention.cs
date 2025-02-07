﻿namespace SIL.Machine.WebApi.DataAccess;

public class ObjectRefConvention : ConventionBase, IMemberMapConvention
{
    public void Apply(BsonMemberMap memberMap)
    {
        if (memberMap.MemberName.EndsWith("Ref"))
            memberMap.SetSerializer(new StringSerializer(BsonType.ObjectId));
    }
}
