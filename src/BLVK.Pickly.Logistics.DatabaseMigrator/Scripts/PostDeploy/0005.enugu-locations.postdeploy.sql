/*
    Enugu canonical streets and landmarks (DRAFT - pending geographic review).

    Source: OpenStreetMap (c) OpenStreetMap contributors, ODbL 1.0, extracted through the Overpass API at
    osm_base 2026-10-18T21:28:20Z. Every row keeps its OSM element as provenance in
    locations."LocationProviderReferences" (Provider 'openstreetmap', FeatureId 'way/<id>' or 'node/<id>'),
    so each record can be checked at https://www.openstreetmap.org/<FeatureId>. ODbL permits permanent
    storage with attribution; see Diagnostics/GEOGRAPHIC-SOURCES.md for the extraction, selection rules and
    exclusions.

    Shape mirrors Location.RegisterProviderResolved with ResolutionSource InternallyRegistered:
      - streets: one canonical row per named street; the point is the midpoint of the street's longest OSM
        segment and FeatureId is the OSM way at that point. Names that occur in more than one place in the
        city are excluded, so different streets are never merged under one name.
      - landmarks: distinct named places (FeatureType 'poi' => LocationType Landmark).
      - AddressLine = name, City/State 'Enugu' (Mapbox context naming), CountryCode 'NG'; NormalizedAddress is
        computed exactly like LocationAddress.NormalizeForIdentity.

    Runs on every deployment as one batch and therefore one transaction.

    Idempotent, insert-only: a row is skipped when its OSM reference is already registered, or when a
    usable Location with the same normalized identity already exists (for example registered earlier through
    Mapbox), so reruns never duplicate canonical locations and never modify, retire or resurrect existing
    ones. Corrections to an already registered location are an explicit, reviewed operation.
*/

SELECT pg_advisory_xact_lock(hashtext('pickly.seed.enugu-locations'));

CREATE TEMP TABLE enugu_location_seed ON COMMIT DROP AS
SELECT
    v.feature_id,
    v.feature_type,
    v.name,
    v.latitude,
    v.longitude,
    v.name || ', Enugu, Enugu State' AS display_address,
    concat_ws('|',
        lower(regexp_replace(btrim(regexp_replace(v.name, '[^[:alnum:][:space:]]+', ' ', 'g')), '\s+', ' ', 'g')),
        'enugu',
        'enugu',
        'ng') AS normalized_address
FROM (
    SELECT feature_id, 'street' AS feature_type, name, latitude, longitude
    FROM (VALUES
    ('way/267006843', 'Ama-etiti Street', 6.4806503, 7.5122433), -- ENU-ABK
    ('way/241657452', 'Amuri Road', 6.4821075, 7.5090785), -- ENU-ABK
    ('way/241647677', 'Chief Edward Nnaji Street', 6.4856946, 7.5176723), -- ENU-ABK
    ('way/240552813', 'College Road', 6.4824642, 7.5275370), -- ENU-ABK
    ('way/267616724', 'Dom Ekete Avenue', 6.5101216, 7.5193218), -- ENU-ABK
    ('way/267618052', 'Dominic Avenue', 6.5200398, 7.5321833), -- ENU-ABK
    ('way/267616590', 'Harmony Road', 6.5000114, 7.5281825), -- ENU-ABK
    ('way/240554249', 'Imo River Street', 6.4814005, 7.5193146), -- ENU-ABK
    ('way/241648655', 'Ogwuagor Road', 6.4881257, 7.5237581), -- ENU-ABK
    ('way/241647589', 'Ugbene Road', 6.4856824, 7.5137539), -- ENU-ABK
    ('way/241657371', 'Ugwunwani Street', 6.4909408, 7.5092459), -- ENU-ABK
    ('way/241654916', 'Ugwuorie Road', 6.4998003, 7.5191002), -- ENU-ABK
    ('way/241649059', 'Umuaga Street', 6.4788698, 7.5180805), -- ENU-ABK
    ('way/518139898', 'Agbani Link Road', 6.4342784, 7.4800287), -- ENU-ACH
    ('way/241652327', 'Amaigbo Lane', 6.4297194, 7.4899908), -- ENU-ACH
    ('way/241648380', 'Chief Emeka Ebila Street', 6.4159336, 7.4872465), -- ENU-ACH
    ('way/518139895', 'Coal Camp Road', 6.4276057, 7.4788975), -- ENU-ACH
    ('way/241651788', 'Edozie Street', 6.4263572, 7.4947884), -- ENU-ACH
    ('way/241648356', 'Idodo Street', 6.4146953, 7.4906383), -- ENU-ACH
    ('way/241648250', 'Mgbemene Street', 6.4331229, 7.4855240), -- ENU-ACH
    ('way/241656261', 'Nise Street', 6.4189118, 7.4924789), -- ENU-ACH
    ('way/631125938', 'Ogbete Main Market Road', 6.4368473, 7.4845128), -- ENU-ACH
    ('way/241648131', 'Ogbunike Street', 6.4241834, 7.4775202), -- ENU-ACH
    ('way/241648403', 'Oji Street', 6.4214182, 7.4895181), -- ENU-ACH
    ('way/241648358', 'Ozalla Street', 6.4159927, 7.4913283), -- ENU-ACH
    ('way/581204790', 'Ziks Avenue', 6.4279642, 7.4936541), -- ENU-ACH
    ('way/241655283', 'Abiriba Avenue', 6.4596468, 7.5609055), -- ENU-EME
    ('way/241651576', 'Adoration Road', 6.4874182, 7.5669845), -- ENU-EME
    ('way/267046563', 'Amechi Street', 6.4704872, 7.5795439), -- ENU-EME
    ('way/241655401', 'Annunciation Hospital Road', 6.4667294, 7.5891544), -- ENU-EME
    ('way/599900858', 'Ben Emeasoba Street', 6.4638930, 7.5724610), -- ENU-EME
    ('way/1430940700', 'Enugu-Abakaliki Road', 6.4648082, 7.5910717), -- ENU-EME
    ('way/241650720', 'Lady Ngozi Umeoji Avenue', 6.4524326, 7.5618066), -- ENU-EME
    ('way/1437148224', 'Lopa Energy Avenue', 6.4504129, 7.5685651), -- ENU-EME
    ('way/241655984', 'Nkwubor Road', 6.5014752, 7.5997904), -- ENU-EME
    ('way/241649781', 'Obiagu Street', 6.4678170, 7.5925047), -- ENU-EME
    ('way/241655859', 'Okorocha Street', 6.4777686, 7.5752084), -- ENU-EME
    ('way/1499147749', 'Proda Road', 6.4381440, 7.5945013), -- ENU-EME
    ('way/1430936084', 'Rehabilitation Road', 6.4876331, 7.5783980), -- ENU-EME
    ('way/241654897', 'Aria Road', 6.4466787, 7.4777522), -- ENU-GRA
    ('way/241653698', 'Ekulu Avenue', 6.4677527, 7.4971621), -- ENU-GRA
    ('way/241648456', 'Hospital Avenue', 6.4588854, 7.4944962), -- ENU-GRA
    ('way/241650597', 'Imoke Street', 6.4690439, 7.5011299), -- ENU-GRA
    ('way/521055666', 'Iva Valley Road', 6.4582759, 7.4732874), -- ENU-GRA
    ('way/241654901', 'National Trade Fair Complex Road', 6.4640465, 7.4812372), -- ENU-GRA
    ('way/241647842', 'Onitsha Road', 6.4599989, 7.4773257), -- ENU-GRA
    ('way/240563062', 'Park Avenue', 6.4638587, 7.5021201), -- ENU-GRA
    ('way/241653714', 'Peacock Drive', 6.4670290, 7.5089435), -- ENU-GRA
    ('way/241654867', 'S.G. Ugwuanyi Close', 6.4613545, 7.4772463), -- ENU-GRA
    ('way/240554118', 'Savage Crescent', 6.4606491, 7.4974297), -- ENU-GRA
    ('way/241647988', 'Sir Mike Omeje Avenue', 6.4627710, 7.4828627), -- ENU-GRA
    ('way/241657557', 'Works Road', 6.4518013, 7.4833195), -- ENU-GRA
    ('way/240554661', 'Aguleri Street', 6.4397029, 7.5216054), -- ENU-IND
    ('way/258902159', 'Ascension Road', 6.4227763, 7.5326910), -- ENU-IND
    ('way/272988970', 'Ezillo Street', 6.4463354, 7.5318194), -- ENU-IND
    ('way/241652006', 'Hon Sunday Ugwu Street', 6.4296815, 7.5536604), -- ENU-IND
    ('way/241657426', 'Independence Avenue', 6.4431884, 7.5316319), -- ENU-IND
    ('way/241648263', 'NTA Road', 6.4390766, 7.5100555), -- ENU-IND
    ('way/241648209', 'Nkpokiti Road', 6.4358574, 7.5116862), -- ENU-IND
    ('way/241652927', 'Obeagu Ugwuaji Awkunanaw Road', 6.4051582, 7.5691683), -- ENU-IND
    ('way/241649148', 'Okpara Square Road', 6.4405545, 7.5199852), -- ENU-IND
    ('way/241657551', 'Presidential Road', 6.4412109, 7.5098922), -- ENU-IND
    ('way/240552809', 'Rangers Avenue', 6.4438478, 7.5151002), -- ENU-IND
    ('way/240562974', 'Umuawulu Street', 6.4310842, 7.5260890), -- ENU-IND
    ('way/241652054', 'Vosan Drive', 6.4401673, 7.5365183), -- ENU-IND
    ('way/259746619', 'Abakaliki Close', 6.4638881, 7.5217593), -- ENU-NHV
    ('way/240563128', 'Chime Avenue', 6.4581254, 7.5221916), -- ENU-NHV
    ('way/241647761', 'Christ Avenue', 6.4595009, 7.5202971), -- ENU-NHV
    ('way/241647747', 'Enweani Street', 6.4624181, 7.5346638), -- ENU-NHV
    ('way/519182730', 'Ibusa Avenue', 6.4493229, 7.5372441), -- ENU-NHV
    ('way/241657360', 'Ihiala Avenue', 6.4605512, 7.5340590), -- ENU-NHV
    ('way/266806673', 'Mbanefo Street', 6.4590239, 7.5203570), -- ENU-NHV
    ('way/241648964', 'Nteje Street', 6.4555513, 7.5299751), -- ENU-NHV
    ('way/241657365', 'Nza Street', 6.4508810, 7.5286287), -- ENU-NHV
    ('way/267617924', 'Prince Michael Den Chukwu Street', 6.4473152, 7.5577726), -- ENU-NHV
    ('way/241647756', 'Uduma Street', 6.4545233, 7.5227666), -- ENU-NHV
    ('way/267179829', 'Umuezebi Street', 6.4547677, 7.5280409), -- ENU-NHV
    ('way/241654547', 'Valley Crescent', 6.4521167, 7.5201761), -- ENU-NHV
    ('way/241657644', 'Annang Street', 6.4336760, 7.5018713), -- ENU-OGU
    ('way/241650338', 'Club Avenue', 6.4467506, 7.4903032), -- ENU-OGU
    ('way/241647572', 'Ebe-Ano Road', 6.4540224, 7.5000417), -- ENU-OGU
    ('way/241657647', 'Kingsway Road', 6.4424002, 7.4811323), -- ENU-OGU
    ('way/241648447', 'Lagos Street', 6.4473586, 7.5040041), -- ENU-OGU
    ('way/258623664', 'Nwafor Orizu Avenue', 6.4476459, 7.5103715), -- ENU-OGU
    ('way/584875840', 'Nwoga Street', 6.4304294, 7.4966275), -- ENU-OGU
    ('way/274720868', 'O''connor Road', 6.4436102, 7.4990986), -- ENU-OGU
    ('way/240556562', 'Obiagu Road', 6.4366988, 7.5009314), -- ENU-OGU
    ('way/240559578', 'Ogbaru Street', 6.4460391, 7.5091571), -- ENU-OGU
    ('way/241657515', 'Ogui Road', 6.4477536, 7.4998734), -- ENU-OGU
    ('way/241657473', 'Okpara Avenue', 6.4465692, 7.4913704), -- ENU-OGU
    ('way/241650527', 'Owerri Road', 6.4452567, 7.4990735), -- ENU-OGU
    ('way/240555731', 'Abor Road', 6.4866326, 7.4797572), -- ENU-TEK
    ('way/241650509', 'Bishop Shannahan Road', 6.4950617, 7.4944349), -- ENU-TEK
    ('way/258733443', 'CBN Road', 6.4788873, 7.4977059), -- ENU-TEK
    ('way/240556567', 'Dhamijah Road', 6.4783546, 7.4941855), -- ENU-TEK
    ('way/267616931', 'Mike Torey Avenue', 6.4747639, 7.4838136), -- ENU-TEK
    ('way/267001901', 'Mission Road', 6.4718350, 7.4765308), -- ENU-TEK
    ('way/241657451', 'Nike Lake Road', 6.4894815, 7.5029295), -- ENU-TEK
    ('way/241653696', 'Nnamdi Azikiwe Avenue', 6.4750736, 7.4935538), -- ENU-TEK
    ('way/241656040', 'Nomeh Drive', 6.4824858, 7.4987554), -- ENU-TEK
    ('way/240555470', 'Oguejiofor Road', 6.4900829, 7.4952780), -- ENU-TEK
    ('way/241649327', 'Opi Avenue', 6.4928006, 7.4821604), -- ENU-TEK
    ('way/274706613', 'Sani Abacha Avenue', 6.4771997, 7.4800117), -- ENU-TEK
    ('way/241648882', 'Ugbene 2 Road', 6.4979742, 7.5036088), -- ENU-TEK
    ('way/241649019', 'Agana Street', 6.4696640, 7.5273266), -- ENU-THC
    ('way/1220077558', 'Airport Road', 6.4588049, 7.5516312), -- ENU-THC
    ('way/258866252', 'Alor Street', 6.4738463, 7.5224175), -- ENU-THC
    ('way/241654302', 'Army Barracks Road', 6.4686102, 7.5198215), -- ENU-THC
    ('way/267616628', 'Asadu Street', 6.4704824, 7.5340578), -- ENU-THC
    ('way/274706624', 'Chief Emeka H. Arinze Street', 6.4669202, 7.5515018), -- ENU-THC
    ('way/599307838', 'Ekpeluchi Avenue', 6.4719891, 7.5276451), -- ENU-THC
    ('way/241657463', 'Enugu Onitsha Road Interlink', 6.4751462, 7.5143704), -- ENU-THC
    ('way/241655374', 'Ezuth Street', 6.4634259, 7.5529694), -- ENU-THC
    ('way/241655581', 'Iji Road', 6.4694253, 7.5292207), -- ENU-THC
    ('way/241649015', 'Jideofor Street', 6.4683461, 7.5265606), -- ENU-THC
    ('way/241657373', 'Old Abakaliki Road', 6.4639580, 7.5421671), -- ENU-THC
    ('way/241649710', 'Prince Greg Eneh Street', 6.4682751, 7.5504830), -- ENU-THC
    ('way/241652417', 'Amechi Road', 6.3995770, 7.5018060), -- ENU-UWA
    ('way/241648412', 'Awkunanaw Street', 6.4155791, 7.4995543), -- ENU-UWA
    ('way/241656266', 'Bishop Ayogu Street', 6.4224020, 7.4952508), -- ENU-UWA
    ('way/586377195', 'Centenary Main Road', 6.3979219, 7.5263097), -- ENU-UWA
    ('way/240562591', 'Ebony Paint Road', 6.3935815, 7.4993736), -- ENU-UWA
    ('way/241656890', 'Isuochi Street', 6.4197467, 7.4932220), -- ENU-UWA
    ('way/596616850', 'Kenyatta Street', 6.4231317, 7.4976613), -- ENU-UWA
    ('way/241656925', 'Monaque Avenue', 6.4143287, 7.5226730), -- ENU-UWA
    ('way/240558390', 'One Day Road', 6.3989720, 7.4863048), -- ENU-UWA
    ('way/258750178', 'Osadebe Street', 6.4267733, 7.4970995), -- ENU-UWA
    ('way/241656667', 'Owa Enugu Road', 6.3855182, 7.5100893), -- ENU-UWA
    ('way/258769917', 'Owelli Street', 6.4144623, 7.5023950), -- ENU-UWA
    ('way/241651777', 'UNEC Road', 6.4267476, 7.5008128)  -- ENU-UWA
    ) AS streets(feature_id, name, latitude, longitude)
    UNION ALL
    SELECT feature_id, 'poi', name, latitude, longitude
    FROM (VALUES
    ('way/1433362624', 'Abakpa Market', 6.4834721, 7.5162545), -- ENU-ABK marketplace
    ('way/791453166', 'Abakpa Nike Health Centre', 6.4796201, 7.5159044), -- ENU-ABK hospital
    ('node/2455133522', 'Methodist College', 6.4192080, 7.4873650), -- ENU-ACH college
    ('node/8753705199', 'Ngwo Park', 6.4223693, 7.4924814), -- ENU-ACH park
    ('node/13170601832', 'Ogbete Main Market', 6.4356538, 7.4844225), -- ENU-ACH marketplace
    ('node/2455133709', 'School Of Nursing UNTH', 6.4369220, 7.4781140), -- ENU-ACH college
    ('way/548692606', 'Akanu Ibiam International Airport', 6.4750846, 7.5577709), -- ENU-EME aerodrome
    ('node/8753773220', 'Caritas University', 6.5037334, 7.5692315), -- ENU-EME university
    ('node/8753773222', 'Eke Obinagu Market', 6.4687068, 7.6010143), -- ENU-EME marketplace
    ('way/791453181', 'Colliery Hospital', 6.4493196, 7.4774650), -- ENU-GRA hospital
    ('way/796958283', 'ESUT College of Medicine Enugu', 6.4623007, 7.4947013), -- ENU-GRA university
    ('node/2460130988', 'Enugu State College Of Education (Technical), Enugu', 6.4600750, 7.4986360), -- ENU-GRA college
    ('way/791453192', 'Mbanefo Hospital', 6.4730611, 7.5046212), -- ENU-GRA hospital
    ('node/8753662624', 'National Museum of Unity', 6.4530724, 7.4866157), -- ENU-GRA museum
    ('node/8753646612', 'New Market', 6.4519199, 7.4803376), -- ENU-GRA marketplace
    ('way/518124114', 'Polo Park Mall', 6.4601783, 7.4959705), -- ENU-GRA mall
    ('node/2455133419', 'The Courtyard Hotels & Resorts', 6.4555290, 7.4877040), -- ENU-GRA hotel
    ('way/439048461', 'City Park Luxury Hotel', 6.4224676, 7.5186576), -- ENU-IND hotel
    ('way/1062910020', 'Hotel Presidential', 6.4413529, 7.5125018), -- ENU-IND hotel
    ('way/1421633153', 'Institute of Management and Technology (IMT)', 6.4300108, 7.5134000), -- ENU-IND university
    ('node/2455133622', 'National Library', 6.4323870, 7.5184360), -- ENU-IND library
    ('way/792054615', 'Niger Foundation Hospital &Diagnostic Center', 6.4430439, 7.5135802), -- ENU-IND hospital
    ('way/1421633152', 'Okpara Square', 6.4410453, 7.5191522), -- ENU-IND square
    ('way/1420853006', 'Unity Park', 6.4412500, 7.5215947), -- ENU-IND park
    ('node/8753246687', 'Discovery Plaza', 6.4577077, 7.5236002), -- ENU-NHV mall
    ('node/2455133879', 'Hotel Fidelma', 6.4506570, 7.5364050), -- ENU-NHV hotel
    ('way/792054618', 'Neo Hospital', 6.4513143, 7.5238781), -- ENU-NHV hospital
    ('node/2455133732', 'Hotel Metropole', 6.4388390, 7.4936550), -- ENU-OGU hotel
    ('way/792054613', 'Mother Of Christ Specialist Hospital', 6.4356649, 7.4890754), -- ENU-OGU hospital
    ('way/617584126', 'Nnamdi Azikiwe Stadium', 6.4442601, 7.4964734), -- ENU-OGU stadium
    ('node/8753676026', 'Old Artisan Market', 6.4538382, 7.5011176), -- ENU-OGU marketplace
    ('node/8753833961', 'Police Command Headquarters', 6.4446140, 7.4849790), -- ENU-OGU police
    ('node/2455133699', 'State Central Library', 6.4359650, 7.4912710), -- ENU-OGU library
    ('way/791453168', 'Alpha Specialist Hospital', 6.4790177, 7.4942630), -- ENU-TEK hospital
    ('way/791453174', 'Christ The King Hospital', 6.4794149, 7.4868631), -- ENU-TEK hospital
    ('node/8753782923', 'Memfys Hospital', 6.4771182, 7.4926629), -- ENU-TEK hospital
    ('way/791453169', 'St Gabriel''s Specialist Hospital', 6.4785291, 7.4938352), -- ENU-TEK hospital
    ('node/8753679737', 'National Orthopedic Hospital', 6.4683887, 7.5255560), -- ENU-THC hospital
    ('way/791724805', 'Gerald Hospital', 6.4145041, 7.5093793), -- ENU-UWA hospital
    ('node/13170601825', 'Kenyatta Market', 6.4231493, 7.4984186), -- ENU-UWA marketplace
    ('way/1125508400', 'Mariere Bus Stop', 6.4256650, 7.5069221), -- ENU-UWA bus_station
    ('node/9644326190', 'UNEC Stadium', 6.4267831, 7.4977860), -- ENU-UWA stadium
    ('way/1421633156', 'University of Nigeria Enugu Campus (UNEC)', 6.4232556, 7.5045338)  -- ENU-UWA university
    ) AS landmarks(feature_id, name, latitude, longitude)
) v;

/* Refuse to write anything if the seed itself is inconsistent. */
DO $$
DECLARE
    problems text;
BEGIN
    SELECT string_agg(issue, '; ') INTO problems
    FROM (
        SELECT 'duplicate OSM reference ' || feature_id AS issue FROM enugu_location_seed GROUP BY feature_id HAVING count(*) > 1
        UNION ALL
        SELECT 'duplicate normalized identity ' || normalized_address FROM enugu_location_seed GROUP BY normalized_address HAVING count(*) > 1
        UNION ALL
        SELECT 'coordinates outside Enugu (check latitude/longitude order) for ' || feature_id FROM enugu_location_seed
        WHERE latitude NOT BETWEEN 6.30 AND 6.70 OR longitude NOT BETWEEN 7.40 AND 7.70
        UNION ALL
        SELECT 'more than 7 decimal places for ' || feature_id FROM enugu_location_seed
        WHERE round(latitude, 7) <> latitude OR round(longitude, 7) <> longitude
    ) issues;

    IF problems IS NOT NULL THEN
        RAISE EXCEPTION 'Enugu location seed rejected: %', problems;
    END IF;
END $$;

WITH missing AS MATERIALIZED (
    SELECT gen_random_uuid() AS location_id, s.*
    FROM enugu_location_seed s
    WHERE NOT EXISTS (
            SELECT 1 FROM locations."LocationProviderReferences" r
            WHERE r."Provider" = 'openstreetmap' AND r."FeatureId" = s.feature_id)
      AND NOT EXISTS (
            SELECT 1 FROM locations."Locations" l
            WHERE l."NormalizedAddress" = s.normalized_address AND l."IsDeleted" = FALSE)
),
inserted_locations AS (
    INSERT INTO locations."Locations" ("Id", "DisplayAddress", "NormalizedAddress", "AddressLine", "City", "State", "CountryCode", "Coordinates", "LocationType", "ResolutionSource", "ResolutionStatus", "Visibility", "ResolvedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "IsDeleted")
    SELECT
        m.location_id, m.display_address, m.normalized_address, m.name, 'Enugu', 'Enugu', 'NG',
        ST_SetSRID(ST_MakePoint(m.longitude, m.latitude), 4326)::geography,
        CASE m.feature_type WHEN 'poi' THEN 'Landmark' ELSE 'Street' END,
        'InternallyRegistered', 'Resolved', 'Public',
        TIMESTAMPTZ '2026-10-18 21:28:20+00', NOW(), 'system', NOW(), FALSE
    FROM missing m
    RETURNING "Id"
)
INSERT INTO locations."LocationProviderReferences" ("Id", "LocationId", "Provider", "FeatureId", "FeatureType", "StorageClassification", "ResolvedAtUtc", "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "IsDeleted")
SELECT gen_random_uuid(), m.location_id, 'openstreetmap', m.feature_id, m.feature_type, 'PermanentGeocoding',
       TIMESTAMPTZ '2026-10-18 21:28:20+00', NOW(), 'system', NOW(), FALSE
FROM missing m
JOIN inserted_locations i ON i."Id" = m.location_id;
