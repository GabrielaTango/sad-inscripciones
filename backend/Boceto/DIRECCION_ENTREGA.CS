using System.Data;
using System.Globalization;
using SQLH = Comunes.SQLH;

namespace Negocio.Modelos
{
    public  class DIRECCION_ENTREGA
    {
        public int ID_DIRECCION_ENTREGA { get; set; }
        public string COD_DIRECCION_ENTREGA { get; set; }
        public string COD_CLIENTE { get; set; }
        public string DIRECCION { get; set; }
        public string COD_PROVINCIA { get; set; }
        public string LOCALIDAD { get; set; }
        public string HABITUAL { get; set; }
        public string CODIGO_POSTAL { get; set; }
        public string TELEFONO1 { get; set; }
        public string TELEFONO2 { get; set; }
        public string TOMA_IMPUESTO_HABITUAL { get; set; }
        public string FILLER { get; set; }
        public string OBSERVACIONES { get; set; }
        public int AL_FIJ_IB3 { get; set; }
        public string ALI_ADI_IB { get; set; }
        public string ALI_FIJ_IB { get; set; }
        public int IB_L { get; set; }
        public int IB_L3 { get; set; }
        public int II_IB3 { get; set; }
        public string LIB { get; set; }
        public decimal PORC_L { get; set; }
        public string HABILITADO { get; set; }
        public string HORARIO_ENTREGA { get; set; }
        public string ENTREGA_LUNES { get; set; }
        public string ENTREGA_MARTES { get; set; }
        public string ENTREGA_MIERCOLES { get; set; }
        public string ENTREGA_JUEVES { get; set; }
        public string ENTREGA_VIERNES { get; set; }
        public string ENTREGA_SABADO { get; set; }
        public string ENTREGA_DOMINGO { get; set; }
        public string CONSIDERA_IVA_BASE_CALCULO_IIBB { get; set; }
        public string CONSIDERA_IVA_BASE_CALCULO_IIBB_ADIC { get; set; }
        public int WEB_ADDRESS_ID { get; set; }

        public string Insert()
        {
            string sql = @" INSERT INTO DIRECCION_ENTREGA 
                                (
                                ID_DIRECCION_ENTREGA
                                ,COD_DIRECCION_ENTREGA
                                ,COD_CLIENTE
                                ,DIRECCION
                                ,COD_PROVINCIA
                                ,LOCALIDAD
                                ,HABITUAL
                                ,CODIGO_POSTAL
                                ,TELEFONO1
                                ,TELEFONO2
                                ,TOMA_IMPUESTO_HABITUAL
                                ,FILLER
                                ,OBSERVACIONES
                                ,AL_FIJ_IB3
                                ,ALI_ADI_IB
                                ,ALI_FIJ_IB
                                ,IB_L
                                ,IB_L3
                                ,II_IB3
                                ,LIB
                                ,PORC_L
                                ,HABILITADO
                                ,HORARIO_ENTREGA
                                ,ENTREGA_LUNES
                                ,ENTREGA_MARTES
                                ,ENTREGA_MIERCOLES
                                ,ENTREGA_JUEVES
                                ,ENTREGA_VIERNES
                                ,ENTREGA_SABADO
                                ,ENTREGA_DOMINGO
                                ,CONSIDERA_IVA_BASE_CALCULO_IIBB
                                ,CONSIDERA_IVA_BASE_CALCULO_IIBB_ADIC
                                ,WEB_ADDRESS_ID
                                ,ID_GVA14
                                ,ID_GVA18
                                ) VALUES
                                (
                                NEXT VALUE FOR SEQUENCE_DIRECCION_ENTREGA
                                ," + SQLH.ToString(COD_DIRECCION_ENTREGA) + @" 
                                ," + SQLH.ToString(COD_CLIENTE) + @" 
                                ," + SQLH.ToString(DIRECCION) + @" 
                                ," + SQLH.ToString(COD_PROVINCIA) + @" 
                                ," + SQLH.ToString(LOCALIDAD) + @" 
                                ," + SQLH.ToString(HABITUAL) + @" 
                                ," + SQLH.ToString(CODIGO_POSTAL) + @" 
                                ," + SQLH.ToString(TELEFONO1) + @" 
                                ," + SQLH.ToString(TELEFONO2) + @" 
                                ," + SQLH.ToString(TOMA_IMPUESTO_HABITUAL) + @" 
                                ," + SQLH.ToString(FILLER) + @" 
                                ," + SQLH.ToString(OBSERVACIONES) + @" 
                                ," + SQLH.ToInt(AL_FIJ_IB3) + @" 
                                ," + SQLH.ToString(ALI_ADI_IB) + @" 
                                ," + SQLH.ToString(ALI_FIJ_IB) + @" 
                                ," + SQLH.ToInt(IB_L) + @" 
                                ," + SQLH.ToInt(IB_L3) + @" 
                                ," + SQLH.ToInt(II_IB3) + @" 
                                ," + SQLH.ToString(LIB) + @" 
                                ," + SQLH.ToDecimal(PORC_L) + @" 
                                ," + SQLH.ToString(HABILITADO) + @" 
                                ," + SQLH.ToString(HORARIO_ENTREGA) + @" 
                                ," + SQLH.ToString(ENTREGA_LUNES) + @" 
                                ," + SQLH.ToString(ENTREGA_MARTES) + @" 
                                ," + SQLH.ToString(ENTREGA_MIERCOLES) + @" 
                                ," + SQLH.ToString(ENTREGA_JUEVES) + @" 
                                ," + SQLH.ToString(ENTREGA_VIERNES) + @" 
                                ," + SQLH.ToString(ENTREGA_SABADO) + @" 
                                ," + SQLH.ToString(ENTREGA_DOMINGO) + @" 
                                ," + SQLH.ToString(CONSIDERA_IVA_BASE_CALCULO_IIBB) + @" 
                                ," + SQLH.ToString(CONSIDERA_IVA_BASE_CALCULO_IIBB_ADIC) + @" 
                                ," + SQLH.ToInt(WEB_ADDRESS_ID) + @" 
                                ,(SELECT ID_GVA14 FROM GVA14 WHERE COD_CLIENT = " + SQLH.ToString(COD_CLIENTE) + @")
                                ," + SQLH.TraerId("GVA18","COD_PROVIN",COD_PROVINCIA) + @"
                                )";

            return sql;
        }

        public DIRECCION_ENTREGA()
        {
            DataSet ds = new DataSet();
            ds.ReadXml("XML\\DIRECCION_ENTREGA.xml");

            CultureInfo culture = new CultureInfo("en-US");

            ID_DIRECCION_ENTREGA = int.Parse(ds.Tables[0].Rows[0]["ID_DIRECCION_ENTREGA"].ToString());
            COD_DIRECCION_ENTREGA = ds.Tables[0].Rows[0]["COD_DIRECCION_ENTREGA"].ToString();
            COD_CLIENTE = ds.Tables[0].Rows[0]["COD_CLIENTE"].ToString();
            DIRECCION = ds.Tables[0].Rows[0]["DIRECCION"].ToString();
            COD_PROVINCIA = ds.Tables[0].Rows[0]["COD_PROVINCIA"].ToString();
            LOCALIDAD = ds.Tables[0].Rows[0]["LOCALIDAD"].ToString();
            HABITUAL = ds.Tables[0].Rows[0]["HABITUAL"].ToString();
            CODIGO_POSTAL = ds.Tables[0].Rows[0]["CODIGO_POSTAL"].ToString();
            TELEFONO1 = ds.Tables[0].Rows[0]["TELEFONO1"].ToString();
            TELEFONO2 = ds.Tables[0].Rows[0]["TELEFONO2"].ToString();
            TOMA_IMPUESTO_HABITUAL = ds.Tables[0].Rows[0]["TOMA_IMPUESTO_HABITUAL"].ToString();
            FILLER = ds.Tables[0].Rows[0]["FILLER"].ToString();
            OBSERVACIONES = ds.Tables[0].Rows[0]["OBSERVACIONES"].ToString();
            AL_FIJ_IB3 = int.Parse(ds.Tables[0].Rows[0]["AL_FIJ_IB3"].ToString());
            ALI_ADI_IB = ds.Tables[0].Rows[0]["ALI_ADI_IB"].ToString();
            ALI_FIJ_IB = ds.Tables[0].Rows[0]["ALI_FIJ_IB"].ToString();
            IB_L = int.Parse(ds.Tables[0].Rows[0]["IB_L"].ToString());
            IB_L3 = int.Parse(ds.Tables[0].Rows[0]["IB_L3"].ToString());
            II_IB3 = int.Parse(ds.Tables[0].Rows[0]["II_IB3"].ToString());
            LIB = ds.Tables[0].Rows[0]["LIB"].ToString();
            PORC_L = decimal.Parse(ds.Tables[0].Rows[0]["PORC_L"].ToString(), culture);
            HABILITADO = ds.Tables[0].Rows[0]["HABILITADO"].ToString();
            HORARIO_ENTREGA = ds.Tables[0].Rows[0]["HORARIO_ENTREGA"].ToString();
            ENTREGA_LUNES = ds.Tables[0].Rows[0]["ENTREGA_LUNES"].ToString();
            ENTREGA_MARTES = ds.Tables[0].Rows[0]["ENTREGA_MARTES"].ToString();
            ENTREGA_MIERCOLES = ds.Tables[0].Rows[0]["ENTREGA_MIERCOLES"].ToString();
            ENTREGA_JUEVES = ds.Tables[0].Rows[0]["ENTREGA_JUEVES"].ToString();
            ENTREGA_VIERNES = ds.Tables[0].Rows[0]["ENTREGA_VIERNES"].ToString();
            ENTREGA_SABADO = ds.Tables[0].Rows[0]["ENTREGA_SABADO"].ToString();
            ENTREGA_DOMINGO = ds.Tables[0].Rows[0]["ENTREGA_DOMINGO"].ToString();
            CONSIDERA_IVA_BASE_CALCULO_IIBB = ds.Tables[0].Rows[0]["CONSIDERA_IVA_BASE_CALCULO_IIBB"].ToString();
            CONSIDERA_IVA_BASE_CALCULO_IIBB_ADIC = ds.Tables[0].Rows[0]["CONSIDERA_IVA_BASE_CALCULO_IIBB_ADIC"].ToString();
            WEB_ADDRESS_ID = int.Parse(ds.Tables[0].Rows[0]["WEB_ADDRESS_ID"].ToString());
        }
    }
}
