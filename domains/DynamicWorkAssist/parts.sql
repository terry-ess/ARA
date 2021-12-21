CREATE TABLE SurfaceModels (name TEXT,min_score REAL);
INSERT INTO SurfaceModels VALUES ('parts',.5);
CREATE TABLE Parts (name TEXT,surface_od_model TEXT,surface_od_id INTEGER,surface_min_od_score REAL,nhand_od_model TEXT,nhand_od_id INTEGER,nhand_min_od_score REAL,is_model TEXT,width INTEGER,length INTEGER,height INTEGER,max_dim INTEGER,min_dim INTEGER);
INSERT INTO Parts VALUES ('shaft','parts',2,.5,'objects in hand',1,.5,'shaft',70,10,10,70,10);
INSERT INTO Parts VALUES ('end block','parts',1,.5,'objects in hand',2,.5,'end block',35,20,10,35,20);
