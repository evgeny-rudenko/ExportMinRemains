SELECT       
 dbo.LOT.ID_GOODS, 					-- 1 код товара 
 dbo.GOODS.NAME, 					-- 2 наименование
 dbo.PRODUCER.NAME AS prod_name, 			-- 3 производитель 
 dbo.COUNTRY.NAME AS cnt_name, 				-- 4 страна
 dbo.LOT.QUANTITY_REM, 					-- 5  остаток в текущей единице
 dbo.LOT.PRICE_SUP, 					-- 6 оптовая цена с НДС
 dbo.LOT.PRICE_PROD,					-- 7  цена производителя без НДС
 PRICE_SAL_V_NDS =  dbo.LOT.PRICE_SAL-DBO.LOT.PVAT_SUP,		-- 8 цена поставщика без НДС
 PRICE_PROD_NDS = dbo.LOT.PRICE_PROD*1.1,					-- 9  цена производителя без НДС
 REG_UD ='',								-- 10 номер регистрационного удостоверения
 First_price = 0,							-- 11 цена первого поставщика или импортера -- нет данных
 dbo.LOT.GTD_NUMBER ,						-- 12 номер ГТД
 cert = isnull (dbo.REG_CERT.NAME,'') , 				---13 серитфикат
 SERIES_NUMBER = isnull (dbo.SERIES.SERIES_NUMBER,'') , 				-- 14 серия 
 DBO.LOT.VAT_SUP,							-- 15 ставка НДС
 BEST_BEFORE = isnull ( CONVERT(VARCHAR(10), dbo.SERIES.BEST_BEFORE, 102) , ''), 					-- 16 срок годности
 EXTERNAL_BARCODE = (select top 1 code from BAR_CODE where BAR_CODE.id_goods = dbo.lot.id_goods and DATE_DELETED IS null  ) , -- 17 штрих код производителя
 REGDATE = '',								-- 18 дата регистрации в реестре
 dbo.LOT.REGISTER_PRICE	,					-- 19 реестровая цена 
 SUM_NDS = dbo.LOT.[PVAT_SUP]*dbo.LOT.QUANTITY_REM , -- 20 сумма ндс по строке
 SUMM = dbo.LOT.QUANTITY_REM*dbo.lot.PRICE_SUP, -- 21 сумма по строке с НДС
 dbo.GOODS.IMPORTANT,  					--- 22 ЖВ препарат или не очень
  --PRC_SAL =  replace ('@'+str (dbo.LOT.PRICE_SAL), ' ', ''), -- 23 цена продажи
 PRC_SAL =  dbo.LOT.PRICE_SAL, -- 23 цена продажи
 PROD_DATE = '' ,							-- 24 дату реализации производителем никто не заполнял 
 GTIN = '', -- isnull ( (select top 1  GTIN from v_kiz_list where v_KIZ_LIST.ID_LOT_GLOBAL = DBO.LOT.ID_LOT_GLOBAL), '') ,								-- 25 GTIN
 IS_KIZ = CAST(CASE WHEN LOT.ID_DOCUMENT_ITEM_ADD IS NULL THEN 0 ELSE 1 END AS BIT), -- 26 признак маркировки
 KIZ_LIST = '',-- isnull ( (select   KIZ_STR+',' from v_kiz_list   where V_KIZ_LIST.KIZ_QTY_REM>0 and V_KIZ_LIST.ID_LOT_GLOBAL = DBO.LOT.ID_LOT_GLOBAL for xml path('')) , ''),
 dbo.LOT.INTERNAL_BARCODE, 				-- 28 внутренний ШК партии
 OTDEL = dbo.STORE.name , 
 CONTRACTOR_NAME =CONTRACTOR.NAME,
 CONTRACTOR_ID = CONTRACTOR.ID_CONTRACTOR,
 INVOICE = LOT.INCOMING_NUM,  --(select top 1 INVOICE.INCOMING_NUMBER from INVOICE where INVOICE.ID_INVOICE_GLOBAL = LOT.ID_INVOICE_GLOBAL),
 INVOICE_DATE = isnull ( CONVERT(VARCHAR(10), LOT.INCOMING_DATE, 102) , '')  --(select top 1 INVOICE.INCOMING_DATE from INVOICE where INVOICE.ID_INVOICE_GLOBAL = LOT.ID_INVOICE_GLOBAL),
 FROM            dbo.LOT INNER JOIN
                         dbo.GOODS ON dbo.LOT.ID_GOODS = dbo.GOODS.ID_GOODS INNER JOIN
                         dbo.PRODUCER ON dbo.GOODS.ID_PRODUCER = dbo.PRODUCER.ID_PRODUCER INNER JOIN
                         dbo.STORE ON dbo.LOT.ID_STORE = dbo.STORE.ID_STORE INNER JOIN
						 dbo.CONTRACTOR on dbo.LOT.ID_SUPPLIER = dbo.CONTRACTOR.ID_CONTRACTOR inner join
                         dbo.SCALING_RATIO ON dbo.LOT.ID_SCALING_RATIO = dbo.SCALING_RATIO.ID_SCALING_RATIO AND 
                         dbo.GOODS.ID_GOODS = dbo.SCALING_RATIO.ID_GOODS LEFT OUTER JOIN
                         dbo.SERIES ON dbo.LOT.ID_SERIES = dbo.SERIES.ID_SERIES AND dbo.GOODS.ID_GOODS = dbo.SERIES.ID_GOODS LEFT OUTER JOIN
                         dbo.REG_CERT ON dbo.LOT.ID_REG_CERT_GLOBAL = dbo.REG_CERT.ID_REG_CERT_GLOBAL LEFT OUTER JOIN
                         dbo.COUNTRY ON dbo.PRODUCER.ID_COUNTRY = dbo.COUNTRY.ID_COUNTRY
WHERE        (dbo.LOT.QUANTITY_REM > 0) 
--and  isnull ( (select top 1 status from kiz_status where KIZ_STATUS.DOC = DBO.LOT.INCOMING_NUM), 'Отколнен')!= 'Отклонен'
and CAST(CASE WHEN LOT.ID_DOCUMENT_ITEM_ADD IS NULL THEN 0 ELSE 1 END AS BIT) = 1-- 26 признак маркировки
--and  dbo.STORE.NAME = 'Основной'   ---- подставить нужное наименование 
/*AND (dbo.LOT.ID_SCALING_RATIO IN
                             (SELECT        ID_SCALING_RATIO
                               FROM            dbo.SCALING_RATIO AS SCALING_RATIO_1
                               WHERE        (NUMERATOR = 1) AND (DENOMINATOR = 1)))*/ -- если нужно будет выгрузить только целые, то убрать комментирование
--ORDER BY dbo.GOODS.NAME


