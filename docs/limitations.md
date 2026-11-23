# Limitations

The preview does not analyze dynamic SQL, stored procedures, views without snapshot metadata, `DynamicParameters`, custom type handlers, joins, expressions, null-sensitive values, or full SQL grammar. An implicit conversion is a possible plan risk, not proof of a regression. Validate changes with representative data and SQL Server execution plans.
