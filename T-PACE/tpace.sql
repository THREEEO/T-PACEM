PRAGMA defer_foreign_keys=TRUE;
CREATE TABLE tb_filiais (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    cnpj VARCHAR(14) UNIQUE,
    nome_juridico VARCHAR(150),
    nome_fantasia VARCHAR(150),
    status BOOLEAN,
    inscricao_estadual VARCHAR(20),
    inscricao_municipal VARCHAR(20),
    crt TINYINT,
    caminho_certificado VARCHAR(255),
    senha_certificado VARCHAR(255),
    ambiente_nfe TINYINT,
    cep VARCHAR(8),
    logradouro VARCHAR(100),
    numero VARCHAR(10),
    complemento VARCHAR(50),
    bairro VARCHAR(60),
    cidade VARCHAR(100),
    cod_mun_ibge INTEGER,
    uf CHAR(2),
    telefone VARCHAR(15),
    email VARCHAR(100)
);
INSERT INTO "tb_filiais" ("id","cnpj","nome_juridico","nome_fantasia","status","inscricao_estadual","inscricao_municipal","crt","caminho_certificado","senha_certificado","ambiente_nfe","cep","logradouro","numero","complemento","bairro","cidade","cod_mun_ibge","uf","telefone","email") VALUES(1,'68.712.675/000105','Saint Marie Shoes','Saint Marie Shoes',1,NULL,NULL,NULL,NULL,NULL,NULL,'95720000','R. Buarque de Macedo','3158','','Centro','Garibaldi',NULL,'RS',NULL,NULL);
CREATE TABLE tb_usuarios (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nome VARCHAR(100),
    email VARCHAR(100) UNIQUE,
    senha VARCHAR(255),
    pin VARCHAR(255),
    nivel_acesso INTEGER
, id_filial INTEGER REFERENCES tb_filiais(id));
INSERT INTO "tb_usuarios" ("id","nome","email","senha","pin","nivel_acesso","id_filial") VALUES(1,'Threeeo','contato@threeeo.com.br','tpace','333',1,1);
INSERT INTO "tb_usuarios" ("id","nome","email","senha","pin","nivel_acesso","id_filial") VALUES(2,'Mano','manoelaagostini@hotmail.com','125879','125879',1,1);
INSERT INTO "tb_usuarios" ("id","nome","email","senha","pin","nivel_acesso","id_filial") VALUES(3,'Mari','marieligodziesnki@gmail.com','249581','249581',1,1);
CREATE TABLE tb_clientes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nome VARCHAR(100),
    cpf_cnpj VARCHAR(14) UNIQUE,
    email VARCHAR(100),
    telefone VARCHAR(15),
    cep VARCHAR(8),
    logradouro VARCHAR(100),
    numero VARCHAR(10),
    bairro VARCHAR(60),
    cidade VARCHAR(100),
    complemento VARCHAR(60),
    cod_mun_ibge INTEGER,
    uf CHAR(2)
);
CREATE TABLE tb_fornecedores (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    cnpj VARCHAR(14) UNIQUE,
    nome_juridico VARCHAR(150),
    nome_fantasia VARCHAR(150),
    inscricao_estadual VARCHAR(20),
    cep VARCHAR(8),
    logradouro VARCHAR(100),
    numero VARCHAR(10),
    complemento VARCHAR(60),
    bairro VARCHAR(60),
    cidade VARCHAR(100),
    cod_mun_ibge INTEGER,
    uf CHAR(2),
    telefone VARCHAR(15),
    email VARCHAR(100),
    status BOOLEAN
);
CREATE TABLE tb_caixa (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    id_filial INTEGER,
    nome VARCHAR(30),
    status BOOLEAN,
    FOREIGN KEY(id_filial) REFERENCES tb_filiais(id)
);
CREATE TABLE tb_produtos (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    codigo_barras VARCHAR(20) UNIQUE,
    nome VARCHAR(150),
    custo DECIMAL(10,2),
    preco_venda DECIMAL(10,2),
    ncm VARCHAR(8),
    cest VARCHAR(7),
    aliquotas_imposto DECIMAL(5,2),
    quantidade DECIMAL(10,3),
    valor_promocional DECIMAL(10,2),
    unidade_venda VARCHAR(2) CHECK(unidade_venda IN ('Kg', 'Un', 'L', 'G', 'Mt', 'Cx')),
    em_promocao BOOLEAN,
    lote VARCHAR(50),
    validade DATE,
    id_filial INTEGER, quantidade_minima DECIMAL(10,3) DEFAULT 10, foto text, codigo_geral int(13),
    FOREIGN KEY(id_filial) REFERENCES tb_filiais(id)
);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(1,'7901152539499','Anabela SL',239.95,479.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788634472520_image.jpg',9714585994510);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(2,'7901152524785','Tênis SL caramelo',149.95,299.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788634518739_image.jpg',837645773403);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(3,'7901152527090','Tênis SL nude tela',149.95,299.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788634610933_image.jpg',2426435883209);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(4,'38102','Vanetto Viscoitto/Tresse Tan',169,429.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788642178382_image.jpg',114751348752);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(5,'30330','Sandália Pelo Bamby/Palm Venetto Tan/Salto pelo bamby/tal foco pu pinhao/sola caramelo 5189',165,419.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788642218696_image.jpg',1420438136818);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(6,'7901152509089','Sandália marrom fivela',99.95,199.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788634805816_image.jpg',7866867505688);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(7,'86179','Rasteira Cristal café/palm cristal café/palm cristal café/sola café',219,559.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788642278236_image.jpg',2502506000874);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(8,'30870','Couro Slim Fly Mogno',139.9,349.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788634877292_17886347445095930836955882905501.jpg',8608354246350);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(9,'7900084576398','Tênis RB branco e azul',173.9,399.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788634878028_image.jpg',7956180730291);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(10,'55986','Rasteira bico folha Venetto Bege',149,399.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788642315781_image.jpg',5299002058851);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(11,'7908977350506','Sandália doce de leite SL',174.95,349.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788634945467_image.jpg',8566775168939);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(12,'04211','Sapato Veneto Amendoa/Enf Ouro',149,399.9,NULL,NULL,NULL,8,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641285715_image.jpg',906999789239);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(13,'00229','Sandália Flor Cristal café/cristal café/palm cristal café/salto cristal café/tal foco pu café/sola café com sostura bege',175,439.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788642360135_image.jpg',6638024792685);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(14,'7901152525270','Tênis nude SL',184.95,369.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788635026728_image.jpg',5764005851773);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(15,'27771','Sandália Venetto Amendoa/Palm',199,579.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641329134_image.jpg',2679466839521);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(16,'12886','Sandália pedra Caprino cinamon/palm caprino cinamon/salto caprino cinamon/tal pu coconut/enf ouro resina coconut/sola caramelo 5189',169,429.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788642408868_image.jpg',8941219180732);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(17,'55292','Sandália tiras laços metalic bala ouro light/palm metalic bala ouro light/salto metalic bala ouro light/tal transfer ouro light/sola pele-bege',155,389,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788642468004_image.jpg',7977951991535);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(18,'72647','Sandalia Cam Pig Sky/Palm',229,579.9,NULL,NULL,NULL,7,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641397982_image.jpg',5016954375396);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(20,'7901152530434','Sapatilha white croco SL',99.95,199.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788635300247_image.jpg',4130665940083);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(21,'7900084570952','Tênis RB call Rafaela Boss branco e dourado',166.9,399.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788635319398_image.jpg',2085082720824);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(22,'67363','Rasteira Venetto Biscoito',179,459.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641426837_image.jpg',8130185557167);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(23,'7908806872698','Mule Linho SL',74.95,199.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788635386761_image.jpg',6691295659764);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(24,'71633','Rasteira - Tramado biscoito/venetto biscoito/enf holiday/sola camelo/palm pinhao',165,419.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788642522061_image.jpg',4071400132910);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(25,'20754','Sandália Caprino Preto/Palm',179,459.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641466658_image.jpg',3265907550255);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(26,'7900084571942','Tênis RB',166.9,399.9,NULL,NULL,NULL,3,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788635463207_image.jpg',1421953982315);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(27,'7901152538942','Sandália trama ondas dark cocoa',144.95,289.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788638557545_17886385400097269277266529352366.jpg',224975707347);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(28,'6904083568898','Sapatilha tela nude RB',111.9,279.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788635643570_image.jpg',966392221845);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(29,'69314','Papete Cristal Bronze/Palm',259.9,559.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641520622_image.jpg',2731293563500);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(30,'7900084571072','Tênis nude RB',157.9,379.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788635653267_image.jpg',1524436385034);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(31,'7901152539277','Mule couro nobre soft terracota',134.9,269.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641042392_17886410272794399030341331158174.jpg',7058822222353);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(33,'7900084612416','Tamanco dourado tiras RB',117.9,329.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788635733467_image.jpg',3721071450082);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(34,'70646','Mocassim couro preimum terracota',99.95,199.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641110476_17886411012157439010958456711512.jpg',1957892635525);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(35,'7908954129651','Tênis SL branco',124.95,249.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788635781508_image.jpg',8514277140979);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(36,'77115','Rasteira Arizona Caramelo',199,499.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641565710_image.jpg',5399185895658);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(37,'7901152544325','Espadilhe couro metalic balla ouro cla',149.95,299.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,NULL,7292738151693);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(38,'36865','Rasteira Cristal Bronze/Palm',179,459.9,NULL,NULL,NULL,8,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641613175_image.jpg',7923491440626);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(39,'7901152514106','Tênis SL branco e prata',124.95,249.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788635880682_image.jpg',3990292395324);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(40,'7901152543908','Sandalia couro nobre soft new marfim 2',144.95,289.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641150530_17886411373265279201435208290601.jpg',8769311226076);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(41,'7901152509195','Rasteira dourada SL',74.95,149.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788635927767_image.jpg',9100879796219);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(42,'45347','Rasteira Pelo Zebra/Palm Venetto Sky/Palm',185,469.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641687967_image.jpg',8702063372666);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(43,'7900084580432','Mule RB',95.9,229.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788636023720_image.jpg',9747609252751);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(44,'92865','Rasteira Caprino Avio/Palm',229,579.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641752947_image.jpg',5078169681394);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(45,'7901123811395','bolsa floater preto SL',199.95,379.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,0,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788640078104_image.jpg',5743217483820);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(46,'98981','Slingpack Caprino Preto/Palm',169,429.9,NULL,NULL,NULL,8,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641779783_image.jpg',7105858987171);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(47,'7901099418499','Sapatilha SL rede',124.95,249.9,NULL,NULL,NULL,8,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788636242940_image.jpg',6486765325105);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(48,'7901152531158','Sapatilha doce de leite SL',99.95,199.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788636255391_image.jpg',8173554486429);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(49,'59828','Sapatilha Venetto Tan',195,489.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641844921_image.jpg',9368353155888);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(50,'37401','Slingpack Venetto Capuccino/Palm',199,499.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641950520_image.jpg',6730281301793);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(51,'7901152508518','Sandália caramelo SL',99.95,189.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788636474817_image.jpg',9092059310277);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(52,'42661','Sapatilha Tresse Mc 3784 Whiky 5856 Bt/Venetto Tan/Palm Napa Soft',158,399.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641916828_image.jpg',7015108722089);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(53,'7908896253216','Tênis Palha dourado SL',109.95,219.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788636490260_image.jpg',9621035035596);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(54,'91523','Sandália Metalic Bala Café/Palm',169,429.9,NULL,NULL,NULL,8,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788642019636_image.jpg',3111591573754);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(55,'68305','Rasteira Caprino Cinamo/Enf Ouro Velho/Sola Camelo/Palm Pinhao',189,479.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,NULL,9113936297097);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(56,'7900084560823','Ana bela caramelo RB',124.9,299.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788636627387_image.jpg',8087128494431);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(57,'7901152515370','Tênis camurça nude e branco SL',124.95,249.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788636646311_image.jpg',2887470859760);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(58,'7901123809828','carteira soft black SL',84.95,159.9,NULL,NULL,NULL,2,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788640021673_image.jpg',5617629143474);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(60,'7900084567341','Ana bela branca RB',124.9,299.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788637008618_image.jpg',6258544504958);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(61,'7901123811692','Bolsa Rústico Safari SL',114.95,219.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788637036900_image.jpg',2073908710658);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(63,'7901123811951','Carteira soft preta SL',49.95,199.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788637150238_image.jpg',8980034705453);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(64,'7901123809750','Carteira terracota SL',79.95,199.95,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788637496481_image.jpg',912414619170);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(65,'7901152551576','Anabela folha marrom SL',174.95,349.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788637785428_image.jpg',9929380938638);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(66,'7901152536610','Sandália off SL',84.95,169.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788637910501_image.jpg',7027267130201);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(67,'7901152548712','Anabela nude SL SCARPIN',184.95,369.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788638124534_image.jpg',8279366355140);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(68,'7901152553617','Sandália preto de couro SL',174.95,349.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788640135551_image.jpg',1706967256937);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(69,'7901123810602','Bolsa mini preta SL',134.95,219.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788638343154_image.jpg',8096959416834);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(70,'7901152552016','Scarpin Nude SL',149.95,299.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788638371961_17886382545065640311710074186928.jpg',6391116552971);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(71,'7901152570874','Scarpin SL preto',149.95,299.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788638373697_image.jpg',1385809745351);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(72,'7908937762820','Scarpin verniz show dark cocoa',149.95,329.9,NULL,NULL,NULL,9,NULL,'Un',0,NULL,NULL,1,2,NULL,6889537272237);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(73,'7901123810459','Bolsa tramado ouro SL',64.95,139.9,NULL,NULL,NULL,2,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788638493047_image.jpg',2039694870688);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(74,'7900084568669','Couro torino cognac',99.9,229.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,0,NULL,6109701338291);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(75,'7901123810008','Bolsa P Safari SL',114.95,289.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788638596944_image.jpg',6617183721549);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(76,'7900084561554','Rasteira flor RB',103.9,249.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788638597275_image.jpg',1455549794103);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(77,'7901123809958','Carteira marfim SL',79.95,199.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788638684036_image.jpg',7393896767876);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(78,'7908806818993','Sapatilha preta corrente SL',99.95,199.9,NULL,NULL,NULL,6,NULL,'Un',0,NULL,NULL,1,2,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788638763160_image.jpg',4363283038228);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(79,'79276','Clutch SAFARI SL',74.95,129.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788638882764_image.jpg',9938980566688);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(81,'7901123811197','Bolsa máxi SL',199.95,389.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,0,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788639256282_image.jpg',1066518047246);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(82,'7901123811968','Bolsa P preta SL',114.95,289.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788639279735_image.jpg',3705952374869);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(83,'7901123811081','Bolsa maxi preta SL',199.95,389.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,0,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788639320132_image.jpg',3778693994876);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(84,'7901047939144','Bolsa Tramado Areia SL',64.95,139.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788639403739_17886393854536666477378534432598.jpg',407820496518);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(85,'7901123809811','Carteira Terracota SL',84.95,159.99,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788639532448_image.jpg',713750262771);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(86,'97339','Bolsa palha SL',74.95,179.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,0,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788639573516_image.jpg',7171565832476);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(87,'7901123809934','Carteira Material Lizard Ouro Claro',79.95,199.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788639615281_17886395508732278697327543291271.jpg',8338388212033);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(88,'76802','Carteira fina e longa SL',74.95,129.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,0,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788639773257_image.jpg',1891117576916);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(89,'7908968070246','Carteira caramelo SL',59.95,199.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,0,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788639890130_image.jpg',9720496592079);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(90,'7901123810442','Bolsinha SL OFF',64.95,139.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,0,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788640049824_image.jpg',7372357321633);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(91,'7901123811630','Bolsa Terracota SL',114.95,219.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788640194845_17886401104366060072500652092022.jpg',5961746354792);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(92,'7901123809729','Carteira preta soft SL',79.95,199.9,NULL,NULL,NULL,2,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788640396063_image.jpg',6670309516402);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(93,'7901123812040','Carteira Preta SL',79.95,199.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788640509484_image.jpg',2925284183589);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(94,'7901123811678','Bolsa Preta SL',114.95,219.9,NULL,NULL,NULL,1,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788640536187_17886404350031545753831241411596.jpg',6297354146083);
INSERT INTO "tb_produtos" ("id","codigo_barras","nome","custo","preco_venda","ncm","cest","aliquotas_imposto","quantidade","valor_promocional","unidade_venda","em_promocao","lote","validade","id_filial","quantidade_minima","foto","codigo_geral") VALUES(95,'7901123810497','Bolsa P Soft',64.95,139.9,NULL,NULL,NULL,2,NULL,'Un',0,NULL,NULL,1,1,'https://pub-3c9f79138dbf4db3a3162daf6775e244.r2.dev/1788641067841_17886410557263652818743006922041.jpg',9154801444801);
CREATE TABLE tb_entradas_xml (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    chave_nfe VARCHAR(44) UNIQUE,
    id_fornecedor INTEGER,
    data_entrada DATETIME,
    valor_total DECIMAL(10,2),
    id_filial INTEGER,
    FOREIGN KEY(id_fornecedor) REFERENCES tb_fornecedores(id),
    FOREIGN KEY(id_filial) REFERENCES tb_filiais(id)
);
CREATE TABLE tb_sessao_caixa (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    id_caixa INTEGER,
    id_usuario INTEGER,
    data_abertura DATETIME,
    valor_fundo_troco DECIMAL(10,2),
    data_fechamento DATETIME,
    status BOOLEAN,
    valor_fechamento DECIMAL(10,2),
    FOREIGN KEY(id_caixa) REFERENCES tb_caixa(id),
    FOREIGN KEY(id_usuario) REFERENCES tb_usuarios(id)
);
CREATE TABLE tb_vendas (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    id_sessao_caixa INTEGER,
    id_cliente INTEGER,
    data_hora DATETIME,
    subtotal DECIMAL(10,2),
    desconto DECIMAL(10,2),
    total DECIMAL(10,2),
    status VARCHAR(15) CHECK(status IN ('pago', 'cancelado', 'pendente')),
    chave_nfe VARCHAR(44), id_cupom varchar (25),
    FOREIGN KEY(id_sessao_caixa) REFERENCES tb_sessao_caixa(id),
    FOREIGN KEY(id_cliente) REFERENCES tb_clientes(id)
);
CREATE TABLE tb_pagamentos (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    id_venda INTEGER,
    metodo VARCHAR(20) CHECK(metodo IN ('pix', 'dinheiro', 'cartão de crédito', 'cartão de débito')),
    valor DECIMAL(10,2),
    cod_autorizacao_tef VARCHAR(50),
    FOREIGN KEY(id_venda) REFERENCES tb_vendas(id)
);
CREATE TABLE tb_itens_venda (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    id_venda INTEGER,
    id_produto INTEGER,
    quantidade DECIMAL(10,3),
    preco_unitario DECIMAL(10,2),
    subtotal DECIMAL(10,2),
    FOREIGN KEY(id_venda) REFERENCES tb_vendas(id),
    FOREIGN KEY(id_produto) REFERENCES tb_produtos(id)
);
CREATE TABLE tb_funcionarios (
  id int,
  id_usuario int,
  nome_completo varchar(80) not null,
  data_nascimento date,
  genero tinyint, 
  raca tinyint,
  estado_civil tinyint,
  nacionalidade text(40),
  naturalidade text(40),
  cpf text(11) not null,
  orgao_emissor text(50),
  email text(80),
  telefone text(11),
  contato_emergencia text(11),
  pcd boolean,
  escolaridade tinyint,
  formacao_academica text(40),
  logradouro text(60),
  numero int(4),
  bairro text(40),
  cidade text(60),
  cep text(8),
  complemento text,
  status boolean,
  primary key(id),
  foreign key (id_usuario) references tb_usuarios (id)
);
CREATE TABLE tb_funcionarios_complemento (
  id int,
  id_funcionario int,
  data_admissao date,
  tipo contrato tinyint,
  cargo text,
  nivel_senioridade tinyint,
  setor text,
  gestor int,
  tempo_empregado text,
  modelo_trabalho tinyint,
  escala_trabalho tinyint,
  salario_base real,
  tipo_remuneracao tinyint,
  banco text,
  agencia int,
  chave_pix text,
  centro_custo text,
  data_demissao date,
  tipo_demissao tinyint,
  motivo_demissao text,
  primary key (id),
  foreign key (id_funcionario) references tb_funcionarios (id),
  foreign key (gestor) references tb_funcionarios (id)
);
CREATE TABLE tb_versao (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    versao VARCHAR(20),
    link_download VARCHAR(255),
    data_lancamento DATETIME DEFAULT CURRENT_TIMESTAMP
);
INSERT INTO "tb_versao" ("id","versao","link_download","data_lancamento") VALUES(1,'0.8.0','https://drive.google.com/uc?export=download&id=1uGEIReJgsNaODo7F7xOS0cM-XN05WAam','2026-08-28 02:23:48');
INSERT INTO "tb_versao" ("id","versao","link_download","data_lancamento") VALUES(2,'0.9.0','https://drive.google.com/uc?export=download&id=1CKrjpB4Gzr-cr6lmphm8ZDPUhpkVZQao','2026-08-28 02:36:58');
INSERT INTO "tb_versao" ("id","versao","link_download","data_lancamento") VALUES(3,'0.9.1','https://drive.google.com/uc?export=download&id=1cU8vmyiUPfjRGjVi2tCC4LfietLU5Nsc','2026-08-28 03:04:19');
INSERT INTO "tb_versao" ("id","versao","link_download","data_lancamento") VALUES(4,'0.9.5','https://drive.google.com/uc?export=download&id=12LEtArBu15sU1vwkjgQthuNn4ixZoLQ_','2026-08-28 03:29:01');
INSERT INTO "tb_versao" ("id","versao","link_download","data_lancamento") VALUES(5,'0.9.7','https://drive.google.com/uc?export=download&id=1TiWs6Z_AcjFlv4vCZnIL2HYE3gxXGOWV','2026-08-30 03:21:49');
INSERT INTO "tb_versao" ("id","versao","link_download","data_lancamento") VALUES(6,'0.9.8','https://drive.google.com/uc?export=download&id=1nhynF-KjpIjTmOJUaPAJCScRYD2GTTFS','2026-08-30 04:19:40');
INSERT INTO "tb_versao" ("id","versao","link_download","data_lancamento") VALUES(7,'1.0.0','https://drive.google.com/uc?export=download&id=1bpPdsXQfH9jRYdQ5gCDy_zmAMvxOP28c','2026-08-31 13:46:41');
INSERT INTO "tb_versao" ("id","versao","link_download","data_lancamento") VALUES(8,'1.0.1','https://drive.google.com/uc?export=download&id=1F9fZ1WfMOMdLU3sTYI4CiLjkK7vQy726','2026-08-31 21:13:34');
INSERT INTO "tb_versao" ("id","versao","link_download","data_lancamento") VALUES(9,'1.0.3','https://drive.google.com/uc?export=download&id=1x-Lt1EJInjnOQHUoERqpqvQsYHSzDyuf','2026-09-02 20:10:46');
INSERT INTO "tb_versao" ("id","versao","link_download","data_lancamento") VALUES(10,'1.1','https://drive.google.com/uc?export=download&id=1JwXnziDAdDnMd1y7KuCBsOA6nAowmZ23','2026-09-03 00:31:07');
INSERT INTO "tb_versao" ("id","versao","link_download","data_lancamento") VALUES(11,'1.2','https://drive.google.com/uc?export=download&id=1AIeRumD4NNf6MmV-FJTPW4JN8iqKHhU8','2026-09-04 18:24:35');
INSERT INTO "tb_versao" ("id","versao","link_download","data_lancamento") VALUES(12,'1.2.1','https://drive.google.com/uc?export=download&id=13FKGjLHhJuTDUjTvJ70a_4d_ppatisPn','2026-09-04 18:30:17');
INSERT INTO "tb_versao" ("id","versao","link_download","data_lancamento") VALUES(13,'1.2.2','https://drive.google.com/uc?export=download&id=1R9c91NHpanFCHVPy5jL0kO16pF_FsGLt','2026-09-04 19:57:56');
CREATE TABLE tb_trocas (
    id_troca INTEGER PRIMARY KEY AUTOINCREMENT,
    tipo_troca VARCHAR(15) CHECK(tipo_troca IN ('troca', 'devolucao')),
    data_troca DATETIME DEFAULT CURRENT_TIMESTAMP,
    id_vendedor INTEGER,
    produto_retornado VARCHAR(20),
    quantidade DECIMAL(10,3),
    FOREIGN KEY(produto_retornado) REFERENCES tb_produtos(codigo_barras) ON UPDATE CASCADE ON DELETE CASCADE,
    FOREIGN KEY(id_vendedor) REFERENCES tb_usuarios(id)
);
DELETE FROM sqlite_sequence;
INSERT INTO "sqlite_sequence" ("name","seq") VALUES('tb_versao','13');
INSERT INTO "sqlite_sequence" ("name","seq") VALUES('tb_filiais',1);
INSERT INTO "sqlite_sequence" ("name","seq") VALUES('tb_usuarios',3);
INSERT INTO "sqlite_sequence" ("name","seq") VALUES('tb_produtos',95);
INSERT INTO "sqlite_sequence" ("name","seq") VALUES('tb_trocas',1);
CREATE INDEX idx_usuarios_nivel_acesso ON tb_usuarios(nivel_acesso);
CREATE INDEX idx_fornecedores_nome_fantasia ON tb_fornecedores(nome_fantasia);
CREATE INDEX idx_caixa_id_filial ON tb_caixa(id_filial);
CREATE INDEX idx_produtos_id_filial ON tb_produtos(id_filial);
CREATE INDEX idx_entradas_xml_id_filial ON tb_entradas_xml(id_filial);
CREATE INDEX idx_sessao_caixa_id_caixa ON tb_sessao_caixa(id_caixa);
CREATE INDEX idx_sessao_caixa_id_usuario ON tb_sessao_caixa(id_usuario);
CREATE INDEX idx_sessao_caixa_data_abertura ON tb_sessao_caixa(data_abertura);
CREATE INDEX idx_sessao_caixa_data_fechamento ON tb_sessao_caixa(data_fechamento);
CREATE INDEX idx_vendas_id_sessao_caixa ON tb_vendas(id_sessao_caixa);
CREATE INDEX idx_vendas_id_cliente ON tb_vendas(id_cliente);
CREATE INDEX idx_vendas_data_hora ON tb_vendas(data_hora);
CREATE INDEX idx_pagamentos_id_venda ON tb_pagamentos(id_venda);
CREATE INDEX idx_itens_venda_id_venda ON tb_itens_venda(id_venda);
CREATE INDEX idx_itens_venda_id_produto ON tb_itens_venda(id_produto);
