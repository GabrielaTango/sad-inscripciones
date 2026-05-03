using System;
using System.Data;
using System.Globalization;
using SQLH = Comunes.SQLH;


namespace Negocio.Modelos
{
    public class GVA07
    {
        public int ID_GVA07 { get; set; }
        public string FILLER { get; set; }
        public DateTime FECHA_VTO { get; set; }
        public DateTime F_COMP_CAN { get; set; }
        public decimal IMPORTE_VT { get; set; }
        public decimal IMPORT_CAN { get; set; }
        public int MISMO_CLIE { get; set; }
        public string N_COMP { get; set; }
        public string N_COMP_CAN { get; set; }
        public string T_COMP { get; set; }
        public string T_COMP_CAN { get; set; }
        public decimal IMP_CAN_UN { get; set; }
        public decimal IMP_VT_UNI { get; set; }
        public decimal IMP_VT_EXT { get; set; }
        public decimal IM_CAN_EXT { get; set; }
        public decimal IM_CA_UN_E { get; set; }
        public decimal IM_VT_UN_E { get; set; }
        public int ID_GVA12_CAN { get; set; }
        public int ID_GVA12_FAC { get; set; }
        public string MOTIVO { get; set; }

        public string Insert()
        {
            string sql = @"
                        INSERT INTO GVA07 
                                        (
                                        FILLER
                                        ,FECHA_VTO
                                        ,F_COMP_CAN
                                        ,IMPORTE_VT
                                        ,IMPORT_CAN
                                        ,MISMO_CLIE
                                        ,N_COMP
                                        ,N_COMP_CAN
                                        ,T_COMP
                                        ,T_COMP_CAN
                                        ,IMP_CAN_UN
                                        ,IMP_VT_UNI
                                        ,IMP_VT_EXT
                                        ,IM_CAN_EXT
                                        ,IM_CA_UN_E
                                        ,IM_VT_UN_E
                                        ,ID_GVA12_CAN
                                        ,MOTIVO
                                        ) VALUES
                                        (
                                        " + SQLH.ToString(FILLER) + @" 
                                        ," + SQLH.ToDateTime(FECHA_VTO) + @" 
                                        ," + SQLH.ToDateTime(F_COMP_CAN) + @" 
                                        ," + SQLH.ToDecimal(IMPORTE_VT) + @" 
                                        ," + SQLH.ToDecimal(IMPORT_CAN) + @" 
                                        ," + SQLH.ToInt(MISMO_CLIE) + @" 
                                        ," + SQLH.ToString(N_COMP) + @" 
                                        ," + SQLH.ToString(N_COMP_CAN) + @" 
                                        ," + SQLH.ToString(T_COMP) + @" 
                                        ," + SQLH.ToString(T_COMP_CAN) + @" 
                                        ," + SQLH.ToDecimal(IMP_CAN_UN) + @" 
                                        ," + SQLH.ToDecimal(IMP_VT_UNI) + @" 
                                        ," + SQLH.ToDecimal(IMP_VT_EXT) + @" 
                                        ," + SQLH.ToDecimal(IM_CAN_EXT) + @" 
                                        ," + SQLH.ToDecimal(IM_CA_UN_E) + @" 
                                        ," + SQLH.ToDecimal(IM_VT_UN_E) + @" 
                                        ," + SQLH.ToInt(ID_GVA12_CAN) + @" 
                                        ," + SQLH.ToString(MOTIVO) + @" 
                                        )
                        ";

            return sql;

        }

        public GVA07()
        {
            DataSet ds = new DataSet();
            ds.ReadXml( "XML\\GVA07.xml");

            CultureInfo culture = new CultureInfo("en-US");

            //ID_GVA07 = int.Parse(ds.Tables[0].Rows[0]["ID_GVA07"].ToString());
            FILLER = ds.Tables[0].Rows[0]["FILLER"].ToString();
            FECHA_VTO = DateTime.ParseExact(ds.Tables[0].Rows[0]["FECHA_VTO"].ToString(), "mm/dd/yyyy", CultureInfo.InvariantCulture);
            F_COMP_CAN = DateTime.ParseExact(ds.Tables[0].Rows[0]["F_COMP_CAN"].ToString(), "mm/dd/yyyy", CultureInfo.InvariantCulture);
            IMPORTE_VT = decimal.Parse(ds.Tables[0].Rows[0]["IMPORTE_VT"].ToString(), culture);
            IMPORT_CAN = decimal.Parse(ds.Tables[0].Rows[0]["IMPORT_CAN"].ToString(), culture);
            MISMO_CLIE = int.Parse(ds.Tables[0].Rows[0]["MISMO_CLIE"].ToString());
            N_COMP = ds.Tables[0].Rows[0]["N_COMP"].ToString();
            N_COMP_CAN = ds.Tables[0].Rows[0]["N_COMP_CAN"].ToString();
            T_COMP = ds.Tables[0].Rows[0]["T_COMP"].ToString();
            T_COMP_CAN = ds.Tables[0].Rows[0]["T_COMP_CAN"].ToString();
            IMP_CAN_UN = decimal.Parse(ds.Tables[0].Rows[0]["IMP_CAN_UN"].ToString(), culture);
            IMP_VT_UNI = decimal.Parse(ds.Tables[0].Rows[0]["IMP_VT_UNI"].ToString(), culture);
            IMP_VT_EXT = decimal.Parse(ds.Tables[0].Rows[0]["IMP_VT_EXT"].ToString(), culture);
            IM_CAN_EXT = decimal.Parse(ds.Tables[0].Rows[0]["IM_CAN_EXT"].ToString(), culture);
            IM_CA_UN_E = decimal.Parse(ds.Tables[0].Rows[0]["IM_CA_UN_E"].ToString(), culture);
            IM_VT_UN_E = decimal.Parse(ds.Tables[0].Rows[0]["IM_VT_UN_E"].ToString(), culture);
            ID_GVA12_CAN = int.Parse(ds.Tables[0].Rows[0]["ID_GVA12_CAN"].ToString());
            MOTIVO = ds.Tables[0].Rows[0]["MOTIVO"].ToString();

        }

        internal string CambiarEstadoFac()
        {
            string sql = @"
                            UPDATE GVA12 SET ESTADO = 'CAN',ESTADO_UNI = 'CAN'
                            WHERE ID_GVA12 = " + SQLH.ToInt(ID_GVA12_FAC) + @"
                           ";
            return sql;

        }


        internal string CambiarEstadoVenc()
        {
            string sql = @"
                            UPDATE GVA46 
                                SET ESTADO_VTO = 'PAG',ESTADO_UNI = 'PAG'
                            WHERE T_COMP = " + SQLH.ToString(T_COMP) + @"
                            AND N_COMP = " + SQLH.ToString(N_COMP) + @"
                            AND FECHA_VTO = " + SQLH.ToDateTime(FECHA_VTO) + @"
                           ";
            return sql;

        }

        internal string CambiarEstadoRec()
        {
            string sql = @"
                            UPDATE GVA12 SET ESTADO = 'IMP',ESTADO_UNI = 'IMP'
                            WHERE ID_GVA12 = " + SQLH.ToInt(ID_GVA12_CAN) + @"
                           ";
            return sql;
        }
    }
}
