CREATE TABLE ODmodels (name TEXT,min_score REAL);
INSERT INTO ODmodels VALUES ('containers',.6);
CREATE TABLE Containers (name TEXT,od_model_id INTEGER,od_id INTEGER,is_model TEXT,width INTEGER,length INTEGER,side_height INTEGER,top INTEGER);
INSERT INTO Containers VALUES ('red box',1,1,'box',127,95,35,1);
INSERT INTO Containers VALUES ('blue box',1,2,'box',145,100,35,1);
