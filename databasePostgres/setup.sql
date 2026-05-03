create schema sims;
SET search_path TO sims;
create user appuser;

CREATE TABLE incidentType (
    incident_type_id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    description TEXT
);

CREATE TABLE incident (
    incident_id SERIAL PRIMARY KEY,
    resolved BOOLEAN DEFAULT FALSE,
    reporter VARCHAR(50),
    reported_at TIMESTAMP,
    description TEXT,
    title VARCHAR(100),
    incident_type_id INT REFERENCES incidentType(incident_type_id),
    resource_id VARCHAR(100)
);

CREATE TABLE simsuser (
    user_id SERIAL PRIMARY KEY,
    IsActive BOOLEAN DEFAULT FALSE,
    IsAdmin BOOLEAN DEFAULT FALSE,
    LastLogin TIMESTAMP,
    Username VARCHAR(50),
    PWDHash VARCHAR(200)
);

GRANT USAGE ON SCHEMA sims TO appuser;
GRANT ALL PRIVILEGES
ON ALL SEQUENCES IN SCHEMA sims TO appuser;

GRANT pg_read_all_data TO appuser;

INSERT INTO sims.incidenttype (name) VALUES
   ('ticket'),
   ('issue'),
   ('informational'),
   ('problem');

INSERT INTO sims.incident(
	reporter, reported_at, description, title, incident_type_id, resource_id)
	VALUES ('Admin', CURRENT_TIMESTAMP, 'The whole internet is down,', 'Internet down', '1', 'i-1234567890abcdef0');

INSERT INTO sims.simsuser(
	IsAdmin, IsActive, Username, PWDHash, LastLogin)
	VALUES (true, true, 'admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', CURRENT_TIMESTAMP);

INSERT INTO sims.simsuser(
	IsAdmin, IsActive, Username, PWDHash, LastLogin)
	VALUES (false, true, 'user', '04f8996da763b7a969b1028ee3007569eaf3a635486ddab211d512c85b9df8fb', CURRENT_TIMESTAMP);
