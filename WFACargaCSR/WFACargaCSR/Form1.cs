using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WFACargaCSR.Entities;

namespace WFACargaCSR
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        DB_A3F19C_OGEntities db = new DB_A3F19C_OGEntities();

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnArbrirArchivo_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            txtArchivo.Text = openFileDialog1.FileName;
            MostrarInformacion(txtArchivo.Text);
        }

        private void MostrarInformacion(string filePath)
        {
            try
            {
                DataTable dt = new DataTable();

                string[] lineas = System.IO.File.ReadAllLines(filePath);

                if (lineas.Length > 0)
                {
                    //Archivos Con Encabezado
                    string primeraLinea = lineas[0];
                    string[] encabezados = primeraLinea.Split(',');
                    foreach (string headerWord in encabezados)
                    {

                        if (headerWord.Contains("Piezas"))
                        {
                            dt.Columns.Add(new DataColumn(headerWord, typeof(int)));
                        }
                        else if (headerWord.Contains("Fecha Recolecci�n "))
                        {
                            dt.Columns.Add(new DataColumn(headerWord, typeof(DateTime)));
                        }
                        else
                        {
                            dt.Columns.Add(new DataColumn(headerWord));
                        }
                    }

                    //For Data
                    for (int i = 1; i < lineas.Length; i++)
                    {
                        string[] dataWords = lineas[i].Split(',');
                        DataRow dr = dt.NewRow();
                        int columnIndex = 0;
                        foreach (string palabraEncabezado in encabezados)
                        {
                            dr[palabraEncabezado] = dataWords[columnIndex++];
                        }
                        dt.Rows.Add(dr);
                    }

                    
                    progressBar1.Increment(20);
                    DataTable primeravalidacion = ValidacionPorCuenta(dt);
                    progressBar1.Increment(5);
                    DataTable segundavalidacion = ValidacionReferencia(primeravalidacion);
                    progressBar1.Increment(25);
                    DataTable terceravalicaion = ActualizacionRegistros(segundavalidacion);
                    progressBar1.Increment(25);
                    AgregarRegistrosNuevo(terceravalicaion);
                    progressBar1.Increment(25);

                }
                
                
                if (dt.Rows.Count > 0)
                {
                    dataGridView1.DataSource = dt;
                }

                MessageBox.Show("Se ha completado el proceso.");
            }
            catch (Exception _ex)
            {
                MessageBox.Show("Ha ocurrido el siguiente error: " + _ex.Message.ToString());
            }
        }

        public DataTable ValidacionPorCuenta(DataTable dt) 
        {
            try
            {
                int filas = dt.Rows.Count;

                string cuenta = "980522264";

                foreach (DataRow orow in dt.Select()) 
                {
                    string numerocuenta = orow["No de Cuenta"].ToString();

                    if (!numerocuenta.Contains(cuenta)) 
                    {
                        dt.Rows.Remove(orow);
                    }
                }

                //lblValidacionCuenta.Text = "Validacion de cuentas terminada";

                int filasrestantes = dt.Rows.Count;
                                    
                dt.AcceptChanges();

                return dt;
            }
            catch (Exception _ex)
            {
                MessageBox.Show("Ha ocurrido un error: " + _ex.Message.ToString());
                throw;
            }            
        }

        public DataTable ValidacionReferencia(DataTable dt) 
        {
            try
            {
                int filas = dt.Rows.Count;

                string referencia1 = "KITROPAP";
                string referencia2 = "GIVEAWAY";
                string referencia3 = "2Z";
                string referencia4 = "2T";
                string referencia5 = "50H";
                string referencia6 = "QRO";

                foreach (DataRow orow in dt.Select())
                {
                    string referencia = orow["Referencia"].ToString();
                    
                    if (referencia.Contains(referencia1) ||
                        referencia.Contains(referencia2) ||
                        referencia.Contains(referencia3) ||
                        referencia.Contains(referencia4) ||
                        referencia.Contains(referencia5) ||
                        referencia.Contains(referencia6))
                    {
                        
                    }
                    else
                    {
                        dt.Rows.Remove(orow);
                    }
                }

                dt.AcceptChanges();

                int filasRestante = dt.Rows.Count;                

                return dt;
            }
            catch (Exception _ex)
            {
                MessageBox.Show("Ha ocurrido un error: " + _ex.Message.ToString());
                throw;
            }           
        }

        public DataTable ActualizacionRegistros(DataTable dt) 
        {
            try
            {
                List<csr> listaActualizar = db.csr.OrderByDescending(x => x.id).Take(30000).ToList();

                int filas = dt.Rows.Count;

                foreach (DataRow orow in dt.Select())
                {
                    string referencia = orow["Referencia"].ToString();
                    string ultimock = orow["Ultimo Checkpoint"].ToString();

                    var csrdata = listaActualizar.Where(x => x.Referencia.Contains(referencia)).FirstOrDefault();

                    if (csrdata != null)
                    {
                        if (csrdata.UltimoCheckpoint != "OK")
                        {
                            string query = "UPDATE csr SET [UltimoCheckpoint] = @UltimoCheckpoINT WHERE ([Referencia] LIKE '%' + @Referencia + '%') AND ([UltimoCheckpoint] != 'OK')";
                            db.Database.ExecuteSqlCommand(query, new SqlParameter("@Referencia", referencia), new SqlParameter("@UltimoCheckpoINT", ultimock));
                            dt.Rows.Remove(orow);
                        }
                        else
                        {
                            dt.Rows.Remove(orow);
                        }
                    }
                }

                dt.AcceptChanges();

                int filasRestante = dt.Rows.Count;

                return dt;
            }
            catch (Exception _ex)
            {
                MessageBox.Show("Ha ocurrido un error: " + _ex.Message.ToString());
                throw;
            }
        }       

        public bool AgregarRegistrosNuevo(DataTable dt) 
        {
            int filas = dt.Rows.Count;

            try
            {
                using (SqlConnection con = new SqlConnection("Data Source=sql7001.site4now.net; " + "Initial Catalog=DB_A3F19C_OG;" + "User id=DB_A3F19C_OG_admin;" + "Password=xQ9znAhU;"))
                {
                    using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(con))
                    {
                        sqlBulkCopy.DestinationTableName = "dbo.csr";

                        //Primer Parametro DataTable
                        //Segundo Parametro BD
                        sqlBulkCopy.ColumnMappings.Add("No de Cuenta", "NumeroCuenta");
                        sqlBulkCopy.ColumnMappings.Add("Guia", "Guia");
                        sqlBulkCopy.ColumnMappings.Add("Piece ID", "PieceID");
                        sqlBulkCopy.ColumnMappings.Add("Referencia", "Referencia");
                        sqlBulkCopy.ColumnMappings.Add("IATA Origen", "IATAOrigen");
                        //sqlBulkCopy.ColumnMappings.Add("IATA Origen", "CiudadOrigen");
                        sqlBulkCopy.ColumnMappings.Add("IATA Destino", "IATADestino");
                        //sqlBulkCopy.ColumnMappings.Add("IATA Destino", "CiudadDestino");
                        sqlBulkCopy.ColumnMappings.Add("SVC/SVP", "SVCSubIATA");
                        sqlBulkCopy.ColumnMappings.Add("SVC/SVP", "Ruta");
                        sqlBulkCopy.ColumnMappings.Add("Piezas", "Piezas");
                        sqlBulkCopy.ColumnMappings.Add("Peso", "Peso");
                        sqlBulkCopy.ColumnMappings.Add("Fecha Recoleccion ", "FechaRecoleccion");
                        //sqlBulkCopy.ColumnMappings.Add("Fecha Recolecci�n ", "FechaPrimerCheckpointTerminal");
                        sqlBulkCopy.ColumnMappings.Add("Hora Recoleccion ", "HoraPrimerCheckpointTerminal");
                        //sqlBulkCopy.ColumnMappings.Add("�ltima Incidencia", "PrimerCheckpointTerminal");
                        //sqlBulkCopy.ColumnMappings.Add("�ltima Incidencia", "DescripcionPrimerCheckTerminal");
                        sqlBulkCopy.ColumnMappings.Add("Detalles de entrega / Comentarios", "DetallesEntregaComentarios");
                        //sqlBulkCopy.ColumnMappings.Add("Intentos entrega", "TiempoTransitoEstimado");
                        //sqlBulkCopy.ColumnMappings.Add("Intentos entrega", "TiempoTransitoRealizado");
                        sqlBulkCopy.ColumnMappings.Add("Intentos entrega", "IntentosEntrega");
                        //sqlBulkCopy.ColumnMappings.Add("Detalles de entrega / Comentarios", "CausaDemora");
                        //sqlBulkCopy.ColumnMappings.Add("Fecha CC", "FechaIngresoCC");
                        //sqlBulkCopy.ColumnMappings.Add("D�as en CC", "DiasCC");
                        sqlBulkCopy.ColumnMappings.Add("Producto", "Producto");
                        sqlBulkCopy.ColumnMappings.Add("Valor de Seguro", "ValorSeguro");
                        sqlBulkCopy.ColumnMappings.Add("Nombre Remitente", "NombreRemitente");
                        sqlBulkCopy.ColumnMappings.Add("Contacto Remitente", "ContactoRemitente");
                        //sqlBulkCopy.ColumnMappings.Add("Direcci�n Remitente", "DireccionRemitente");
                        sqlBulkCopy.ColumnMappings.Add("CP Remitente", "CPRemitente");
                        sqlBulkCopy.ColumnMappings.Add("Nombre Destinatario", "NombreDestinatario");
                        sqlBulkCopy.ColumnMappings.Add("Contacto Destinatario", "ContactoDestinatario");
                        //sqlBulkCopy.ColumnMappings.Add("Direcci�n Destinatario", "DireccionDestinatario");
                        sqlBulkCopy.ColumnMappings.Add("CP Destinatario", "CPDestinatario");
                        sqlBulkCopy.ColumnMappings.Add("Ultimo Checkpoint", "UltimoCheckpoint");
                        //sqlBulkCopy.ColumnMappings.Add("Fecha del �ltimo checkpoint", "FechaUltimoCheckpoint");
                        //sqlBulkCopy.ColumnMappings.Add("Hora del �ltimo checkpoint", "HoraUltimoCheckpoint");
                        //sqlBulkCopy.ColumnMappings.Add("Detalles de entrega / Comentarios", "detalleultimocheckpoint");
                        sqlBulkCopy.ColumnMappings.Add("FechaCarga", "FechaCarga");

                        con.Open();

                        sqlBulkCopy.WriteToServer(dt);

                        con.Close();
                    }
                }
            }
            catch (Exception _ex)
            {
                MessageBox.Show("asd");
            }        
            
            return false;
        }
    }
}
