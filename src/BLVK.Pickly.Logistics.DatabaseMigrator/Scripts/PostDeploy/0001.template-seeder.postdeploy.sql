/*
    Seeds reference_data."Countries" using Flagpedia country codes and flag URLs,
    and REST Countries dialing codes.
    Sources:
    - https://flagpedia.net/download/api
    - https://restcountries.com/

    CallingCode stores one canonical country calling code per country.
    The canonical value is the source root code.
*/

WITH country_values("ShortCode", "Name", "CallingCode", "FlagUrl") AS (VALUES
    ('AD', 'Andorra', '+3', 'https://flagcdn.com/ad.svg'),
    ('AE', 'United Arab Emirates', '+9', 'https://flagcdn.com/ae.svg'),
    ('AF', 'Afghanistan', '+9', 'https://flagcdn.com/af.svg'),
    ('AG', 'Antigua and Barbuda', '+1', 'https://flagcdn.com/ag.svg'),
    ('AI', 'Anguilla', '+1', 'https://flagcdn.com/ai.svg'),
    ('AL', 'Albania', '+3', 'https://flagcdn.com/al.svg'),
    ('AM', 'Armenia', '+3', 'https://flagcdn.com/am.svg'),
    ('AO', 'Angola', '+2', 'https://flagcdn.com/ao.svg'),
    ('AQ', 'Antarctica', '', 'https://flagcdn.com/aq.svg'),
    ('AR', 'Argentina', '+5', 'https://flagcdn.com/ar.svg'),
    ('AS', 'American Samoa', '+1', 'https://flagcdn.com/as.svg'),
    ('AT', 'Austria', '+4', 'https://flagcdn.com/at.svg'),
    ('AU', 'Australia', '+6', 'https://flagcdn.com/au.svg'),
    ('AW', 'Aruba', '+2', 'https://flagcdn.com/aw.svg'),
    ('AX', 'Åland Islands', '+3', 'https://flagcdn.com/ax.svg'),
    ('AZ', 'Azerbaijan', '+9', 'https://flagcdn.com/az.svg'),
    ('BA', 'Bosnia and Herzegovina', '+3', 'https://flagcdn.com/ba.svg'),
    ('BB', 'Barbados', '+1', 'https://flagcdn.com/bb.svg'),
    ('BD', 'Bangladesh', '+8', 'https://flagcdn.com/bd.svg'),
    ('BE', 'Belgium', '+3', 'https://flagcdn.com/be.svg'),
    ('BF', 'Burkina Faso', '+2', 'https://flagcdn.com/bf.svg'),
    ('BG', 'Bulgaria', '+3', 'https://flagcdn.com/bg.svg'),
    ('BH', 'Bahrain', '+9', 'https://flagcdn.com/bh.svg'),
    ('BI', 'Burundi', '+2', 'https://flagcdn.com/bi.svg'),
    ('BJ', 'Benin', '+2', 'https://flagcdn.com/bj.svg'),
    ('BL', 'Saint Barthélemy', '+5', 'https://flagcdn.com/bl.svg'),
    ('BM', 'Bermuda', '+1', 'https://flagcdn.com/bm.svg'),
    ('BN', 'Brunei', '+6', 'https://flagcdn.com/bn.svg'),
    ('BO', 'Bolivia', '+5', 'https://flagcdn.com/bo.svg'),
    ('BQ', 'Caribbean Netherlands', '+5', 'https://flagcdn.com/bq.svg'),
    ('BR', 'Brazil', '+5', 'https://flagcdn.com/br.svg'),
    ('BS', 'Bahamas', '+1', 'https://flagcdn.com/bs.svg'),
    ('BT', 'Bhutan', '+9', 'https://flagcdn.com/bt.svg'),
    ('BV', 'Bouvet Island', '+4', 'https://flagcdn.com/bv.svg'),
    ('BW', 'Botswana', '+2', 'https://flagcdn.com/bw.svg'),
    ('BY', 'Belarus', '+3', 'https://flagcdn.com/by.svg'),
    ('BZ', 'Belize', '+5', 'https://flagcdn.com/bz.svg'),
    ('CA', 'Canada', '+1', 'https://flagcdn.com/ca.svg'),
    ('CC', 'Cocos (Keeling) Islands', '+6', 'https://flagcdn.com/cc.svg'),
    ('CD', 'DR Congo', '+2', 'https://flagcdn.com/cd.svg'),
    ('CF', 'Central African Republic', '+2', 'https://flagcdn.com/cf.svg'),
    ('CG', 'Republic of the Congo', '+2', 'https://flagcdn.com/cg.svg'),
    ('CH', 'Switzerland', '+4', 'https://flagcdn.com/ch.svg'),
    ('CI', 'Côte d''Ivoire (Ivory Coast)', '+2', 'https://flagcdn.com/ci.svg'),
    ('CK', 'Cook Islands', '+6', 'https://flagcdn.com/ck.svg'),
    ('CL', 'Chile', '+5', 'https://flagcdn.com/cl.svg'),
    ('CM', 'Cameroon', '+2', 'https://flagcdn.com/cm.svg'),
    ('CN', 'China', '+8', 'https://flagcdn.com/cn.svg'),
    ('CO', 'Colombia', '+5', 'https://flagcdn.com/co.svg'),
    ('CR', 'Costa Rica', '+5', 'https://flagcdn.com/cr.svg'),
    ('CU', 'Cuba', '+5', 'https://flagcdn.com/cu.svg'),
    ('CV', 'Cape Verde', '+2', 'https://flagcdn.com/cv.svg'),
    ('CW', 'Curaçao', '+5', 'https://flagcdn.com/cw.svg'),
    ('CX', 'Christmas Island', '+6', 'https://flagcdn.com/cx.svg'),
    ('CY', 'Cyprus', '+3', 'https://flagcdn.com/cy.svg'),
    ('CZ', 'Czechia', '+4', 'https://flagcdn.com/cz.svg'),
    ('DE', 'Germany', '+4', 'https://flagcdn.com/de.svg'),
    ('DJ', 'Djibouti', '+2', 'https://flagcdn.com/dj.svg'),
    ('DK', 'Denmark', '+4', 'https://flagcdn.com/dk.svg'),
    ('DM', 'Dominica', '+1', 'https://flagcdn.com/dm.svg'),
    ('DO', 'Dominican Republic', '+1', 'https://flagcdn.com/do.svg'),
    ('DZ', 'Algeria', '+2', 'https://flagcdn.com/dz.svg'),
    ('EC', 'Ecuador', '+5', 'https://flagcdn.com/ec.svg'),
    ('EE', 'Estonia', '+3', 'https://flagcdn.com/ee.svg'),
    ('EG', 'Egypt', '+2', 'https://flagcdn.com/eg.svg'),
    ('EH', 'Western Sahara', '+2', 'https://flagcdn.com/eh.svg'),
    ('ER', 'Eritrea', '+2', 'https://flagcdn.com/er.svg'),
    ('ES', 'Spain', '+3', 'https://flagcdn.com/es.svg'),
    ('ET', 'Ethiopia', '+2', 'https://flagcdn.com/et.svg'),
    ('FI', 'Finland', '+3', 'https://flagcdn.com/fi.svg'),
    ('FJ', 'Fiji', '+6', 'https://flagcdn.com/fj.svg'),
    ('FK', 'Falkland Islands', '+5', 'https://flagcdn.com/fk.svg'),
    ('FM', 'Micronesia', '+6', 'https://flagcdn.com/fm.svg'),
    ('FO', 'Faroe Islands', '+2', 'https://flagcdn.com/fo.svg'),
    ('FR', 'France', '+3', 'https://flagcdn.com/fr.svg'),
    ('GA', 'Gabon', '+2', 'https://flagcdn.com/ga.svg'),
    ('GB', 'United Kingdom', '+4', 'https://flagcdn.com/gb.svg'),
    ('GD', 'Grenada', '+1', 'https://flagcdn.com/gd.svg'),
    ('GE', 'Georgia', '+9', 'https://flagcdn.com/ge.svg'),
    ('GF', 'French Guiana', '+5', 'https://flagcdn.com/gf.svg'),
    ('GG', 'Guernsey', '+4', 'https://flagcdn.com/gg.svg'),
    ('GH', 'Ghana', '+2', 'https://flagcdn.com/gh.svg'),
    ('GI', 'Gibraltar', '+3', 'https://flagcdn.com/gi.svg'),
    ('GL', 'Greenland', '+2', 'https://flagcdn.com/gl.svg'),
    ('GM', 'Gambia', '+2', 'https://flagcdn.com/gm.svg'),
    ('GN', 'Guinea', '+2', 'https://flagcdn.com/gn.svg'),
    ('GP', 'Guadeloupe', '+5', 'https://flagcdn.com/gp.svg'),
    ('GQ', 'Equatorial Guinea', '+2', 'https://flagcdn.com/gq.svg'),
    ('GR', 'Greece', '+3', 'https://flagcdn.com/gr.svg'),
    ('GS', 'South Georgia', '+5', 'https://flagcdn.com/gs.svg'),
    ('GT', 'Guatemala', '+5', 'https://flagcdn.com/gt.svg'),
    ('GU', 'Guam', '+1', 'https://flagcdn.com/gu.svg'),
    ('GW', 'Guinea-Bissau', '+2', 'https://flagcdn.com/gw.svg'),
    ('GY', 'Guyana', '+5', 'https://flagcdn.com/gy.svg'),
    ('HK', 'Hong Kong', '+8', 'https://flagcdn.com/hk.svg'),
    ('HM', 'Heard Island and McDonald Islands', '', 'https://flagcdn.com/hm.svg'),
    ('HN', 'Honduras', '+5', 'https://flagcdn.com/hn.svg'),
    ('HR', 'Croatia', '+3', 'https://flagcdn.com/hr.svg'),
    ('HT', 'Haiti', '+5', 'https://flagcdn.com/ht.svg'),
    ('HU', 'Hungary', '+3', 'https://flagcdn.com/hu.svg'),
    ('ID', 'Indonesia', '+6', 'https://flagcdn.com/id.svg'),
    ('IE', 'Ireland', '+3', 'https://flagcdn.com/ie.svg'),
    ('IL', 'Israel', '+9', 'https://flagcdn.com/il.svg'),
    ('IM', 'Isle of Man', '+4', 'https://flagcdn.com/im.svg'),
    ('IN', 'India', '+9', 'https://flagcdn.com/in.svg'),
    ('IO', 'British Indian Ocean Territory', '+2', 'https://flagcdn.com/io.svg'),
    ('IQ', 'Iraq', '+9', 'https://flagcdn.com/iq.svg'),
    ('IR', 'Iran', '+9', 'https://flagcdn.com/ir.svg'),
    ('IS', 'Iceland', '+3', 'https://flagcdn.com/is.svg'),
    ('IT', 'Italy', '+3', 'https://flagcdn.com/it.svg'),
    ('JE', 'Jersey', '+4', 'https://flagcdn.com/je.svg'),
    ('JM', 'Jamaica', '+1', 'https://flagcdn.com/jm.svg'),
    ('JO', 'Jordan', '+9', 'https://flagcdn.com/jo.svg'),
    ('JP', 'Japan', '+8', 'https://flagcdn.com/jp.svg'),
    ('KE', 'Kenya', '+2', 'https://flagcdn.com/ke.svg'),
    ('KG', 'Kyrgyzstan', '+9', 'https://flagcdn.com/kg.svg'),
    ('KH', 'Cambodia', '+8', 'https://flagcdn.com/kh.svg'),
    ('KI', 'Kiribati', '+6', 'https://flagcdn.com/ki.svg'),
    ('KM', 'Comoros', '+2', 'https://flagcdn.com/km.svg'),
    ('KN', 'Saint Kitts and Nevis', '+1', 'https://flagcdn.com/kn.svg'),
    ('KP', 'North Korea', '+8', 'https://flagcdn.com/kp.svg'),
    ('KR', 'South Korea', '+8', 'https://flagcdn.com/kr.svg'),
    ('KW', 'Kuwait', '+9', 'https://flagcdn.com/kw.svg'),
    ('KY', 'Cayman Islands', '+1', 'https://flagcdn.com/ky.svg'),
    ('KZ', 'Kazakhstan', '+7', 'https://flagcdn.com/kz.svg'),
    ('LA', 'Laos', '+8', 'https://flagcdn.com/la.svg'),
    ('LB', 'Lebanon', '+9', 'https://flagcdn.com/lb.svg'),
    ('LC', 'Saint Lucia', '+1', 'https://flagcdn.com/lc.svg'),
    ('LI', 'Liechtenstein', '+4', 'https://flagcdn.com/li.svg'),
    ('LK', 'Sri Lanka', '+9', 'https://flagcdn.com/lk.svg'),
    ('LR', 'Liberia', '+2', 'https://flagcdn.com/lr.svg'),
    ('LS', 'Lesotho', '+2', 'https://flagcdn.com/ls.svg'),
    ('LT', 'Lithuania', '+3', 'https://flagcdn.com/lt.svg'),
    ('LU', 'Luxembourg', '+3', 'https://flagcdn.com/lu.svg'),
    ('LV', 'Latvia', '+3', 'https://flagcdn.com/lv.svg'),
    ('LY', 'Libya', '+2', 'https://flagcdn.com/ly.svg'),
    ('MA', 'Morocco', '+2', 'https://flagcdn.com/ma.svg'),
    ('MC', 'Monaco', '+3', 'https://flagcdn.com/mc.svg'),
    ('MD', 'Moldova', '+3', 'https://flagcdn.com/md.svg'),
    ('ME', 'Montenegro', '+3', 'https://flagcdn.com/me.svg'),
    ('MF', 'Saint Martin', '+5', 'https://flagcdn.com/mf.svg'),
    ('MG', 'Madagascar', '+2', 'https://flagcdn.com/mg.svg'),
    ('MH', 'Marshall Islands', '+6', 'https://flagcdn.com/mh.svg'),
    ('MK', 'North Macedonia', '+3', 'https://flagcdn.com/mk.svg'),
    ('ML', 'Mali', '+2', 'https://flagcdn.com/ml.svg'),
    ('MM', 'Myanmar', '+9', 'https://flagcdn.com/mm.svg'),
    ('MN', 'Mongolia', '+9', 'https://flagcdn.com/mn.svg'),
    ('MO', 'Macau', '+8', 'https://flagcdn.com/mo.svg'),
    ('MP', 'Northern Mariana Islands', '+1', 'https://flagcdn.com/mp.svg'),
    ('MQ', 'Martinique', '+5', 'https://flagcdn.com/mq.svg'),
    ('MR', 'Mauritania', '+2', 'https://flagcdn.com/mr.svg'),
    ('MS', 'Montserrat', '+1', 'https://flagcdn.com/ms.svg'),
    ('MT', 'Malta', '+3', 'https://flagcdn.com/mt.svg'),
    ('MU', 'Mauritius', '+2', 'https://flagcdn.com/mu.svg'),
    ('MV', 'Maldives', '+9', 'https://flagcdn.com/mv.svg'),
    ('MW', 'Malawi', '+2', 'https://flagcdn.com/mw.svg'),
    ('MX', 'Mexico', '+5', 'https://flagcdn.com/mx.svg'),
    ('MY', 'Malaysia', '+6', 'https://flagcdn.com/my.svg'),
    ('MZ', 'Mozambique', '+2', 'https://flagcdn.com/mz.svg'),
    ('NA', 'Namibia', '+2', 'https://flagcdn.com/na.svg'),
    ('NC', 'New Caledonia', '+6', 'https://flagcdn.com/nc.svg'),
    ('NE', 'Niger', '+2', 'https://flagcdn.com/ne.svg'),
    ('NF', 'Norfolk Island', '+6', 'https://flagcdn.com/nf.svg'),
    ('NG', 'Nigeria', '+2', 'https://flagcdn.com/ng.svg'),
    ('NI', 'Nicaragua', '+5', 'https://flagcdn.com/ni.svg'),
    ('NL', 'Netherlands', '+3', 'https://flagcdn.com/nl.svg'),
    ('NO', 'Norway', '+4', 'https://flagcdn.com/no.svg'),
    ('NP', 'Nepal', '+9', 'https://flagcdn.com/np.svg'),
    ('NR', 'Nauru', '+6', 'https://flagcdn.com/nr.svg'),
    ('NU', 'Niue', '+6', 'https://flagcdn.com/nu.svg'),
    ('NZ', 'New Zealand', '+6', 'https://flagcdn.com/nz.svg'),
    ('OM', 'Oman', '+9', 'https://flagcdn.com/om.svg'),
    ('PA', 'Panama', '+5', 'https://flagcdn.com/pa.svg'),
    ('PE', 'Peru', '+5', 'https://flagcdn.com/pe.svg'),
    ('PF', 'French Polynesia', '+6', 'https://flagcdn.com/pf.svg'),
    ('PG', 'Papua New Guinea', '+6', 'https://flagcdn.com/pg.svg'),
    ('PH', 'Philippines', '+6', 'https://flagcdn.com/ph.svg'),
    ('PK', 'Pakistan', '+9', 'https://flagcdn.com/pk.svg'),
    ('PL', 'Poland', '+4', 'https://flagcdn.com/pl.svg'),
    ('PM', 'Saint Pierre and Miquelon', '+5', 'https://flagcdn.com/pm.svg'),
    ('PN', 'Pitcairn Islands', '+6', 'https://flagcdn.com/pn.svg'),
    ('PR', 'Puerto Rico', '+1', 'https://flagcdn.com/pr.svg'),
    ('PS', 'Palestine', '+9', 'https://flagcdn.com/ps.svg'),
    ('PT', 'Portugal', '+3', 'https://flagcdn.com/pt.svg'),
    ('PW', 'Palau', '+6', 'https://flagcdn.com/pw.svg'),
    ('PY', 'Paraguay', '+5', 'https://flagcdn.com/py.svg'),
    ('QA', 'Qatar', '+9', 'https://flagcdn.com/qa.svg'),
    ('RE', 'Réunion', '+2', 'https://flagcdn.com/re.svg'),
    ('RO', 'Romania', '+4', 'https://flagcdn.com/ro.svg'),
    ('RS', 'Serbia', '+3', 'https://flagcdn.com/rs.svg'),
    ('RU', 'Russia', '+7', 'https://flagcdn.com/ru.svg'),
    ('RW', 'Rwanda', '+2', 'https://flagcdn.com/rw.svg'),
    ('SA', 'Saudi Arabia', '+9', 'https://flagcdn.com/sa.svg'),
    ('SB', 'Solomon Islands', '+6', 'https://flagcdn.com/sb.svg'),
    ('SC', 'Seychelles', '+2', 'https://flagcdn.com/sc.svg'),
    ('SD', 'Sudan', '+2', 'https://flagcdn.com/sd.svg'),
    ('SE', 'Sweden', '+4', 'https://flagcdn.com/se.svg'),
    ('SG', 'Singapore', '+6', 'https://flagcdn.com/sg.svg'),
    ('SH', 'Saint Helena, Ascension and Tristan da Cunha', '+2', 'https://flagcdn.com/sh.svg'),
    ('SI', 'Slovenia', '+3', 'https://flagcdn.com/si.svg'),
    ('SJ', 'Svalbard and Jan Mayen', '+4', 'https://flagcdn.com/sj.svg'),
    ('SK', 'Slovakia', '+4', 'https://flagcdn.com/sk.svg'),
    ('SL', 'Sierra Leone', '+2', 'https://flagcdn.com/sl.svg'),
    ('SM', 'San Marino', '+3', 'https://flagcdn.com/sm.svg'),
    ('SN', 'Senegal', '+2', 'https://flagcdn.com/sn.svg'),
    ('SO', 'Somalia', '+2', 'https://flagcdn.com/so.svg'),
    ('SR', 'Suriname', '+5', 'https://flagcdn.com/sr.svg'),
    ('SS', 'South Sudan', '+2', 'https://flagcdn.com/ss.svg'),
    ('ST', 'São Tomé and Príncipe', '+2', 'https://flagcdn.com/st.svg'),
    ('SV', 'El Salvador', '+5', 'https://flagcdn.com/sv.svg'),
    ('SX', 'Sint Maarten', '+1', 'https://flagcdn.com/sx.svg'),
    ('SY', 'Syria', '+9', 'https://flagcdn.com/sy.svg'),
    ('SZ', 'Eswatini (Swaziland)', '+2', 'https://flagcdn.com/sz.svg'),
    ('TC', 'Turks and Caicos Islands', '+1', 'https://flagcdn.com/tc.svg'),
    ('TD', 'Chad', '+2', 'https://flagcdn.com/td.svg'),
    ('TF', 'French Southern and Antarctic Lands', '+2', 'https://flagcdn.com/tf.svg'),
    ('TG', 'Togo', '+2', 'https://flagcdn.com/tg.svg'),
    ('TH', 'Thailand', '+6', 'https://flagcdn.com/th.svg'),
    ('TJ', 'Tajikistan', '+9', 'https://flagcdn.com/tj.svg'),
    ('TK', 'Tokelau', '+6', 'https://flagcdn.com/tk.svg'),
    ('TL', 'Timor-Leste', '+6', 'https://flagcdn.com/tl.svg'),
    ('TM', 'Turkmenistan', '+9', 'https://flagcdn.com/tm.svg'),
    ('TN', 'Tunisia', '+2', 'https://flagcdn.com/tn.svg'),
    ('TO', 'Tonga', '+6', 'https://flagcdn.com/to.svg'),
    ('TR', 'Turkey', '+9', 'https://flagcdn.com/tr.svg'),
    ('TT', 'Trinidad and Tobago', '+1', 'https://flagcdn.com/tt.svg'),
    ('TV', 'Tuvalu', '+6', 'https://flagcdn.com/tv.svg'),
    ('TW', 'Taiwan', '+8', 'https://flagcdn.com/tw.svg'),
    ('TZ', 'Tanzania', '+2', 'https://flagcdn.com/tz.svg'),
    ('UA', 'Ukraine', '+3', 'https://flagcdn.com/ua.svg'),
    ('UG', 'Uganda', '+2', 'https://flagcdn.com/ug.svg'),
    ('UM', 'United States Minor Outlying Islands', '+2', 'https://flagcdn.com/um.svg'),
    ('US', 'United States', '+1', 'https://flagcdn.com/us.svg'),
    ('UY', 'Uruguay', '+5', 'https://flagcdn.com/uy.svg'),
    ('UZ', 'Uzbekistan', '+9', 'https://flagcdn.com/uz.svg'),
    ('VA', 'Vatican City (Holy See)', '+3', 'https://flagcdn.com/va.svg'),
    ('VC', 'Saint Vincent and the Grenadines', '+1', 'https://flagcdn.com/vc.svg'),
    ('VE', 'Venezuela', '+5', 'https://flagcdn.com/ve.svg'),
    ('VG', 'British Virgin Islands', '+1', 'https://flagcdn.com/vg.svg'),
    ('VI', 'United States Virgin Islands', '+1', 'https://flagcdn.com/vi.svg'),
    ('VN', 'Vietnam', '+8', 'https://flagcdn.com/vn.svg'),
    ('VU', 'Vanuatu', '+6', 'https://flagcdn.com/vu.svg'),
    ('WF', 'Wallis and Futuna', '+6', 'https://flagcdn.com/wf.svg'),
    ('WS', 'Samoa', '+6', 'https://flagcdn.com/ws.svg'),
    ('XK', 'Kosovo', '+3', 'https://flagcdn.com/xk.svg'),
    ('YE', 'Yemen', '+9', 'https://flagcdn.com/ye.svg'),
    ('YT', 'Mayotte', '+2', 'https://flagcdn.com/yt.svg'),
    ('ZA', 'South Africa', '+2', 'https://flagcdn.com/za.svg'),
    ('ZM', 'Zambia', '+2', 'https://flagcdn.com/zm.svg'),
    ('ZW', 'Zimbabwe', '+2', 'https://flagcdn.com/zw.svg')
),
countries_existing AS (
    SELECT t."ShortCode"
    FROM reference_data."Countries" t
    INNER JOIN country_values v ON t."ShortCode" = v."ShortCode"
),
countries_updated AS (
    UPDATE reference_data."Countries" t
    SET
        "Name" = v."Name",
        "CallingCode" = v."CallingCode",
        "FlagUrl" = v."FlagUrl",
        "UpdatedAtUtc" = NOW()
    FROM country_values v
    WHERE t."ShortCode" = v."ShortCode"
      AND (t."Name" IS DISTINCT FROM v."Name"
           OR t."CallingCode" IS DISTINCT FROM v."CallingCode"
           OR t."FlagUrl" IS DISTINCT FROM v."FlagUrl")
)
INSERT INTO reference_data."Countries" ("Id", "ShortCode", "Name", "CallingCode", "FlagUrl", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT gen_random_uuid(), v."ShortCode", v."Name", v."CallingCode", v."FlagUrl", NOW(), NOW(), FALSE
FROM country_values v
WHERE NOT EXISTS (SELECT 1 FROM countries_existing e WHERE e."ShortCode" = v."ShortCode");

UPDATE infrastructure."Providers"
SET
    "IsActive" = FALSE,
    "UpdatedAtUtc" = NOW()
WHERE "ProviderType" = 1
    AND "ProviderKey" <> 'mailtrap'
    AND "IsActive" = TRUE;

UPDATE infrastructure."Providers"
SET
    "IsActive" = FALSE,
    "IsDeleted" = TRUE,
    "DeletedAtUtc" = NOW(),
    "DeletedBy" = 'system',
    "UpdatedAtUtc" = NOW()
WHERE "ProviderType" = 1
    AND "ProviderKey" = 'logging'
    AND "IsDeleted" = FALSE;

WITH provider_values("ProviderType", "ProviderName", "ProviderKey", "IsActive") AS (VALUES
    (1, 'Mailtrap', 'mailtrap', TRUE),
    (2, 'Noop (Stub)', 'noop', FALSE),
    (2, 'Cloudflare R2', 'cloudflare_r2', TRUE)
),
providers_existing AS (
    SELECT t."ProviderType", t."ProviderKey"
    FROM infrastructure."Providers" t
    INNER JOIN provider_values v
        ON t."ProviderType" = v."ProviderType" AND t."ProviderKey" = v."ProviderKey"
),
providers_updated AS (
    UPDATE infrastructure."Providers" t
    SET
        "ProviderName" = v."ProviderName",
        "IsActive" = v."IsActive",
        "UpdatedAtUtc" = NOW()
    FROM provider_values v
    WHERE t."ProviderType" = v."ProviderType"
      AND t."ProviderKey" = v."ProviderKey"
      AND (t."ProviderName" IS DISTINCT FROM v."ProviderName"
           OR t."IsActive" IS DISTINCT FROM v."IsActive")
)
INSERT INTO infrastructure."Providers" ("Id", "ProviderType", "ProviderName", "ProviderKey", "IsActive", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT gen_random_uuid(), v."ProviderType", v."ProviderName", v."ProviderKey", v."IsActive", NOW(), NOW(), FALSE
FROM provider_values v
WHERE NOT EXISTS (
    SELECT 1 FROM providers_existing e
    WHERE e."ProviderType" = v."ProviderType" AND e."ProviderKey" = v."ProviderKey"
);

WITH payment_provider_values("ProviderName", "ProviderKey", "IsActive") AS (VALUES
    ('SafeHaven', 'safehaven', TRUE),
    ('Credo', 'credo', TRUE)
),
payment_providers_existing AS (
    SELECT t."ProviderKey"
    FROM payments."PaymentProviders" t
    INNER JOIN payment_provider_values v ON t."ProviderKey" = v."ProviderKey"
),
payment_providers_updated AS (
    UPDATE payments."PaymentProviders" t
    SET
        "ProviderName" = v."ProviderName",
        "IsActive" = v."IsActive",
        "UpdatedAtUtc" = NOW()
    FROM payment_provider_values v
    WHERE t."ProviderKey" = v."ProviderKey"
      AND (t."ProviderName" IS DISTINCT FROM v."ProviderName"
           OR t."IsActive" IS DISTINCT FROM v."IsActive")
)
INSERT INTO payments."PaymentProviders" ("Id", "ProviderName", "ProviderKey", "IsActive", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT gen_random_uuid(), v."ProviderName", v."ProviderKey", v."IsActive", NOW(), NOW(), FALSE
FROM payment_provider_values v
WHERE NOT EXISTS (SELECT 1 FROM payment_providers_existing e WHERE e."ProviderKey" = v."ProviderKey");

WITH currency_values("CurrencyCode", "CurrencyName", "IsActive") AS (VALUES
    ('NGN', 'Naira', TRUE),
    ('USD', 'US Dollar', TRUE)
),
currencies_existing AS (
    SELECT t."CurrencyCode"
    FROM payments."Currencies" t
    INNER JOIN currency_values v ON t."CurrencyCode" = v."CurrencyCode"
),
currencies_updated AS (
    UPDATE payments."Currencies" t
    SET
        "CurrencyName" = v."CurrencyName",
        "IsActive" = v."IsActive",
        "UpdatedAtUtc" = NOW()
    FROM currency_values v
    WHERE t."CurrencyCode" = v."CurrencyCode"
      AND (t."CurrencyName" IS DISTINCT FROM v."CurrencyName"
           OR t."IsActive" IS DISTINCT FROM v."IsActive")
)
INSERT INTO payments."Currencies" ("Id", "CurrencyCode", "CurrencyName", "IsActive", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT gen_random_uuid(), v."CurrencyCode", v."CurrencyName", v."IsActive", NOW(), NOW(), FALSE
FROM currency_values v
WHERE NOT EXISTS (SELECT 1 FROM currencies_existing e WHERE e."CurrencyCode" = v."CurrencyCode");

WITH country_currency_source AS (
    SELECT
        gen_random_uuid() AS id,
        c."Id" AS country_id,
        cu."Id" AS currency_id,
        TRUE AS is_default,
        TRUE AS is_active
    FROM reference_data."Countries" c
    INNER JOIN payments."Currencies" cu
        ON cu."CurrencyCode" =
            CASE
                WHEN c."ShortCode" = 'NG' THEN 'NGN'
                ELSE 'USD'
            END
),
country_currencies_existing AS (
    SELECT t."CountryId", t."CurrencyId"
    FROM payments."CountryCurrencies" t
    INNER JOIN country_currency_source s
        ON t."CountryId" = s.country_id AND t."CurrencyId" = s.currency_id
),
country_currencies_updated AS (
    UPDATE payments."CountryCurrencies" t
    SET
        "IsDefault" = s.is_default,
        "IsActive" = s.is_active,
        "UpdatedAtUtc" = NOW()
    FROM country_currency_source s
    WHERE t."CountryId" = s.country_id
      AND t."CurrencyId" = s.currency_id
      AND (t."IsDefault" IS DISTINCT FROM s.is_default
           OR t."IsActive" IS DISTINCT FROM s.is_active)
)
INSERT INTO payments."CountryCurrencies" ("Id", "CountryId", "CurrencyId", "IsDefault", "IsActive", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT s.id, s.country_id, s.currency_id, s.is_default, s.is_active, NOW(), NOW(), FALSE
FROM country_currency_source s
WHERE NOT EXISTS (
    SELECT 1 FROM country_currencies_existing e
    WHERE e."CountryId" = s.country_id AND e."CurrencyId" = s.currency_id
);

WITH provider_config_source AS (
    SELECT
        pp."Id" AS payment_provider_id,
        c."Id" AS currency_id,
        cfg."PaymentIntent",
        cfg."PaymentMethodType",
        cfg."IsEnabled"
    FROM (VALUES
        ('safehaven', 'NGN', 1, 2, TRUE),
        ('safehaven', 'NGN', 2, 2, TRUE),
        ('credo', 'NGN', 1, 1, TRUE),
        ('credo', 'NGN', 2, 1, TRUE)
    ) AS cfg("ProviderKey", "CurrencyCode", "PaymentIntent", "PaymentMethodType", "IsEnabled")
    INNER JOIN payments."PaymentProviders" pp ON pp."ProviderKey" = cfg."ProviderKey"
    INNER JOIN payments."Currencies" c ON c."CurrencyCode" = cfg."CurrencyCode"
),
provider_configs_existing AS (
    SELECT t."PaymentProviderId", t."CurrencyId", t."PaymentIntent"
    FROM payments."PaymentProviderConfigurations" t
    INNER JOIN provider_config_source s
        ON t."PaymentProviderId" = s.payment_provider_id
        AND t."CurrencyId" = s.currency_id
        AND t."PaymentIntent" = s."PaymentIntent"
),
provider_configs_updated AS (
    UPDATE payments."PaymentProviderConfigurations" t
    SET
        "PaymentMethodType" = s."PaymentMethodType",
        "IsEnabled" = s."IsEnabled",
        "UpdatedAtUtc" = NOW()
    FROM provider_config_source s
    WHERE t."PaymentProviderId" = s.payment_provider_id
      AND t."CurrencyId" = s.currency_id
      AND t."PaymentIntent" = s."PaymentIntent"
      AND (t."PaymentMethodType" IS DISTINCT FROM s."PaymentMethodType"
           OR t."IsEnabled" IS DISTINCT FROM s."IsEnabled")
)
INSERT INTO payments."PaymentProviderConfigurations" ("Id", "PaymentProviderId", "CurrencyId", "PaymentIntent", "PaymentMethodType", "IsEnabled", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT gen_random_uuid(), s.payment_provider_id, s.currency_id, s."PaymentIntent", s."PaymentMethodType", s."IsEnabled", NOW(), NOW(), FALSE
FROM provider_config_source s
WHERE NOT EXISTS (
    SELECT 1 FROM provider_configs_existing e
    WHERE e."PaymentProviderId" = s.payment_provider_id
      AND e."CurrencyId" = s.currency_id
      AND e."PaymentIntent" = s."PaymentIntent"
);

/*
    Seeds stakeholders."Tenants".

    The default tenant (Guid.Empty) is used as a fallback for brand resolution
    when a specific tenant id does not exist.
*/

WITH tenant_values("Id", "Name", "BrandKey") AS (VALUES
    ('1203d9d1-2a6b-48ef-9cc1-e561a23aff72'::uuid, 'Default Tenant', 'default')
),
tenants_existing AS (
    SELECT t."Id"
    FROM stakeholders."Tenants" t
    INNER JOIN tenant_values v ON t."Id" = v."Id"
),
tenants_updated AS (
    UPDATE stakeholders."Tenants" t
    SET
        "Name" = v."Name",
        "BrandKey" = v."BrandKey",
        "UpdatedAtUtc" = NOW()
    FROM tenant_values v
    WHERE t."Id" = v."Id"
      AND (t."Name" IS DISTINCT FROM v."Name"
           OR t."BrandKey" IS DISTINCT FROM v."BrandKey")
)
INSERT INTO stakeholders."Tenants" ("Id", "Name", "BrandKey", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT v."Id", v."Name", v."BrandKey", NOW(), NOW(), FALSE
FROM tenant_values v
WHERE NOT EXISTS (SELECT 1 FROM tenants_existing e WHERE e."Id" = v."Id");

/*
    Seeds stakeholders."StakeholderTypes".

    Defines the default stakeholder types available in the system.
*/

WITH stakeholder_type_values("Id", "TenantId", "Name", "Key") AS (VALUES
    ('65018401-f34e-422a-ad65-ed4b4a5ed266'::uuid, '1203d9d1-2a6b-48ef-9cc1-e561a23aff72'::uuid, 'Customer', 'customer')
),
stakeholder_types_existing AS (
    SELECT t."TenantId", t."Key"
    FROM stakeholders."StakeholderTypes" t
    INNER JOIN stakeholder_type_values v
        ON t."TenantId" = v."TenantId" AND t."Key" = v."Key"
),
stakeholder_types_updated AS (
    UPDATE stakeholders."StakeholderTypes" t
    SET
        "Name" = v."Name",
        "UpdatedAtUtc" = NOW()
    FROM stakeholder_type_values v
    WHERE t."TenantId" = v."TenantId"
      AND t."Key" = v."Key"
      AND t."Name" IS DISTINCT FROM v."Name"
)
INSERT INTO stakeholders."StakeholderTypes" ("Id", "TenantId", "Name", "Key", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT v."Id", v."TenantId", v."Name", v."Key", NOW(), NOW(), FALSE
FROM stakeholder_type_values v
WHERE NOT EXISTS (
    SELECT 1 FROM stakeholder_types_existing e
    WHERE e."TenantId" = v."TenantId" AND e."Key" = v."Key"
);

/*
    Seeds notifications."EmailNotificationTemplates".

    Subject supports named placeholders in the form {{:PlaceholderName:}}.
    The template file name points to an HTML content fragment loaded from filesystem.
*/

WITH template_values("NotificationType", "Language", "Description", "Subject", "TemplateFileName") AS (VALUES
    (1, 'en', 'Account created notification', 'Welcome to {{:Product:}}', 'AccountCreated.html'),
    (2, 'en', 'Email confirmation OTP notification', 'Please confirm your email', 'ConfirmEmail.html'),
    (3, 'en', 'Reset password OTP notification', 'Reset your password', 'ResetPassword.html'),
    (4, 'en', 'Password reset success notification', 'Your password has been reset', 'PasswordResetSuccessful.html'),
    (5, 'en', 'Email confirmation follow-up notification', 'Reminder to confirm your email', 'EmailConfirmationFollowUp.html'),
    (6, 'en', 'Sign-in successful notification', 'Successful sign-in', 'SignInSuccessful.html'),
    (7, 'en', 'Account locked notification', 'Your account has been locked', 'AccountLocked.html'),
    (8, 'en', 'Trial expired notification', 'Your {{:Product:}} trial has ended', 'TrialExpired.html'),
    (9, 'en', 'Subscription cancelled notification', 'Help us improve {{:Product:}}', 'CancelledSubscription.html'),
    (10, 'en', 'Subscription invoice notification', 'Your invoice from {{:Product:}}', 'Invoice.html'),
    (1, 'fr', 'Notification de création de compte', 'Bienvenue sur {{:Product:}}', 'AccountCreated.html'),
    (2, 'fr', 'Notification de confirmation de l’adresse e-mail', 'Confirmez votre adresse e-mail', 'ConfirmEmail.html'),
    (3, 'fr', 'Notification de réinitialisation du mot de passe', 'Réinitialisez votre mot de passe', 'ResetPassword.html'),
    (4, 'fr', 'Notification de réinitialisation réussie', 'Votre mot de passe a été réinitialisé', 'PasswordResetSuccessful.html'),
    (5, 'fr', 'Rappel de confirmation de l’adresse e-mail', 'Rappel : confirmez votre adresse e-mail', 'EmailConfirmationFollowUp.html'),
    (6, 'fr', 'Notification de connexion réussie', 'Connexion réussie', 'SignInSuccessful.html'),
    (7, 'fr', 'Notification de verrouillage du compte', 'Votre compte a été verrouillé', 'AccountLocked.html'),
    (8, 'fr', 'Notification de fin d’essai', 'Votre essai {{:Product:}} est terminé', 'TrialExpired.html'),
    (9, 'fr', 'Notification d’annulation d’abonnement', 'Aidez-nous à améliorer {{:Product:}}', 'CancelledSubscription.html'),
    (10, 'fr', 'Notification de facture', 'Votre facture {{:Product:}}', 'Invoice.html')
),
templates_existing AS (
    SELECT t."NotificationType", t."Language"
    FROM notifications."EmailNotificationTemplates" t
    INNER JOIN template_values v ON t."NotificationType" = v."NotificationType" AND t."Language" = v."Language"
),
templates_updated AS (
    UPDATE notifications."EmailNotificationTemplates" t
    SET
        "Description" = v."Description",
        "Subject" = v."Subject",
        "TemplateFileName" = v."TemplateFileName",
        "UpdatedAtUtc" = NOW()
    FROM template_values v
    WHERE t."NotificationType" = v."NotificationType"
      AND t."Language" = v."Language"
      AND (t."Description" IS DISTINCT FROM v."Description"
           OR t."Subject" IS DISTINCT FROM v."Subject"
           OR t."TemplateFileName" IS DISTINCT FROM v."TemplateFileName")
)
INSERT INTO notifications."EmailNotificationTemplates" ("Id", "NotificationType", "Language", "Description", "Subject", "TemplateFileName", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
SELECT gen_random_uuid(), v."NotificationType", v."Language", v."Description", v."Subject", v."TemplateFileName", NOW(), NOW(), FALSE
FROM template_values v
WHERE NOT EXISTS (SELECT 1 FROM templates_existing e WHERE e."NotificationType" = v."NotificationType" AND e."Language" = v."Language");
