using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Data.SqlClient;

namespace TurbulenceService
{
    /// <summary>
    /// Summary description for DataInfo
    /// </summary>
    public class DataInfo
    {   /*Enumerations have to be specified at compile time.  We will have to change the way this works */
        /* public enum DataSets : int
        {
            mhd1024 = 3,
            isotropic1024coarse = 4,
            isotropic1024fine = 5,
            channel = 6,
            mixing = 7
        }
         */
        public int DataSets;
        public static int findDataSetInt(string DataSetName)
        {
            //SqlConnection sqlcon;
            String connectionString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;
            //sqlcon = new SqlConnection(connectionString);
            using (SqlConnection sqlcon = new SqlConnection(connectionString))
            {
                sqlcon.Open();
                using (SqlCommand cmd = new SqlCommand(
                    String.Format("select DatasetID from turbinfo.dbo.datasets where name= '{0}'", DataSetName), sqlcon))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        if (reader.HasRows) return reader.GetInt32(0);

                        else throw new Exception("Invalid dataset specified!");
                    }
                }
            }
        }
        public static string findDataSetName(int dataset_id)
        {
            String connectionString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;
            using (SqlConnection sqlcon = new SqlConnection(connectionString))
            {
                sqlcon.Open();
                using (SqlCommand cmd = new SqlCommand(
                    String.Format("select name from turbinfo.dbo.datasets where datasetID= {0}", dataset_id), sqlcon))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        if (reader.HasRows) return reader.GetString(0);

                        else throw new Exception("Invalid dataset specified!");
                    }
                }
            }
        }
        public static Boolean isUserCreated(int dataset_id)
        {
            String connectionString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;
            using (SqlConnection sqlcon = new SqlConnection(connectionString))
            {
                sqlcon.Open();
                using (SqlCommand cmd = new SqlCommand(
                    String.Format("select isUserCreated from turbinfo.dbo.datasets where datasetID= {0}", dataset_id), sqlcon))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        if (reader.HasRows) return reader.GetBoolean(0);

                        else throw new Exception("Invalid dataset specified!");
                    }
                }
            }
        }
        public String tablename;

        public static int getNumberComponents(String tableName)
        {   
            String connectionString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;
            
            using (SqlConnection sqlcon = new SqlConnection(connectionString))
            {
                sqlcon.Open();
                using (SqlCommand cmd = new SqlCommand(
                       String.Format("select components from turbinfo.dbo.datafields where tablename = '{0}'", tableName), sqlcon))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        if (reader.HasRows) return reader.GetInt32(0);

                        else throw new Exception("Invalid dataset specified!");
                    }
                }
            }
        }
        public string TableNames;
        public static string getTableName(string dataSet, string field)
        {
            String connectionString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;
            field = field.ToLower(); /*Set field to lowercase*/
            using (SqlConnection sqlcon = new SqlConnection(connectionString))
            {
                sqlcon.Open();
                using (SqlCommand cmd = new SqlCommand(
                           String.Format("select tablename from turbinfo.dbo.datafields, turbinfo.dbo.datasets where datasets.name = '{0}' and datafields.datasetid = datasets.datasetID and (datafields.name ='{1}' or datafields.charname ='{1}' or datafields.longname = '{1}')", dataSet, field), sqlcon))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        if (reader.HasRows) return reader.GetString(0);

                        else
                        {
                            
                            throw new Exception("Invalid dataset specified: " + dataSet + " field: " + field);
                        }
                    }
                }
            }
        }
        public static string GetCharFieldName(string field)
        {
            String connectionString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;
            field = field.ToLower(); /*Set field to lowercase*/
           
            using (SqlConnection sqlcon = new SqlConnection(connectionString))
            {
                sqlcon.Open();
                using (SqlCommand cmd = new SqlCommand(
                           String.Format("select charname from turbinfo.dbo.datafields where (datafields.name ='{1}' or datafields.charname ='{1}' or datafields.longname = '{1}')", field), sqlcon))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        if (reader.HasRows)
                        {
                            string datasetname = reader.GetString(0);
                            sqlcon.Close();
                            return datasetname;
                        }
                        else
                        {
                             
                            throw new Exception("Invalid data set specified!");
                        }
                    }
                }
            }

        }
      
        public static string findDataSet(string setname)
        {
            String connectionString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;

            if (setname != null)
            {
                using (SqlConnection sqlcon = new SqlConnection(connectionString))
                {
                    sqlcon.Open();
                    using (SqlCommand cmd = new SqlCommand(
                               String.Format("select datasetID, name from turbinfo.dbo.datasets where name='{0}'", setname), sqlcon))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            reader.Read();
                            if (reader.HasRows)
                            {
                                return reader.GetString(1);
                            }
                            else { throw new Exception(String.Format("Invalid set name")); }

                        }
                    }
                }
            }
            else
            {
                throw new Exception(String.Format("Invalid set name: {0}", setname));
            }
        }

        public static bool isTimeInRange(int dataset_id, float time)
        {
            if (time < 0)
            {
                return false;
            }
            else
            {
                
                String connectionString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;
                using (SqlConnection sqlcon = new SqlConnection(connectionString))
                {
                    sqlcon.Open();
                    using (SqlCommand cmd = new SqlCommand(
                              String.Format("select maxTime from turbinfo.dbo.datasets where DatasetID= {0}", dataset_id), sqlcon))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            reader.Read();
                            if (reader.HasRows)
                            {
                                float maxTime = (float)reader.GetDouble(0);

                                if (time > maxTime)
                                {
                                    return false;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }

            }              
            return true;
        }

        public static void verifyTimeInRange(int dataset, float time)
        {
            if (!isTimeInRange(dataset, time))
            {
                throw new Exception(String.Format("Time {0} out of range for \"{1}\".", time, dataset));
            }
        }

        public static void verifyRawDataParameters(int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth)
        {
            if ((X % 8 != 0) || (Y % 8 != 0) || (Z % 8 != 0) || (Xwidth % 8 != 0) || (Zwidth % 8 != 0) || (Ywidth % 8 != 0))
            {
                throw new Exception(String.Format("One of X, Y, Z, Xwidth, Ywidth, Zwidth is not a multiple of 8!Currently " +
                    " getting raw data works only for positions and sizes that are a multiple of 8!"));
            }
        }

    }
}