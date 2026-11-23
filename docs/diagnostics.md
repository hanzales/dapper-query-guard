# DQG001

DQG001 identifies a static Dapper call whose parameter appears to bind as Unicode while a non-nullable `varchar` or `char` column is known from the snapshot. It is a potential implicit-conversion risk. Confirm collation, indexes, parameter length and execution plans before changing code.
