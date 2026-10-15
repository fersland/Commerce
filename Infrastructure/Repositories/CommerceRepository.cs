using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Domain.Entities;

namespace Infrastructure.Repositories
{
    public class CommerceRepository
    {
        private readonly string _connectionString;

        public CommerceRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["db"]?.ConnectionString ?? "";
        }

        public void CreateCommerce(Commerce commerce)
        {
            using (var _connection = new SqlConnection(_connectionString))
            {
                using (var _command = new SqlCommand("sp_create_commerce", _connection))
                {
                    _command.CommandType = CommandType.StoredProcedure;
                    _command.Parameters.AddWithValue("@pc_processdate", commerce.PcProcessDate ?? (object)DBNull.Value);
                    _command.Parameters.AddWithValue("@pc_nomcomred", commerce.PcNomComRed ?? (object)DBNull.Value);
                    _command.Parameters.AddWithValue("@pc_numdoc", commerce.PcNumDoc ?? (object)DBNull.Value);

                    _connection.Open();
                    _command.ExecuteNonQuery();
                }
            }
        }

        public int ProcessCommerceValidation(string processDate)
        {
            int totalCuarentena = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var _command = new SqlCommand("sp_process_commerce_validation", connection))
                {
                    _command.CommandType = CommandType.StoredProcedure;
                    _command.Parameters.AddWithValue("@p_processdate", processDate);

                    SqlParameter _salida = new SqlParameter("@TotalQuarantine", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    _command.Parameters.Add(_salida);

                    connection.Open();
                    _command.ExecuteNonQuery();

                    if (_salida.Value != DBNull.Value)
                    {
                        totalCuarentena = Convert.ToInt32(_salida.Value);
                    }
                }
            }

            return totalCuarentena;
        }

        public List<CommerceQuarantine> GetQuarantineList()
        {
            var list = new List<CommerceQuarantine>();

            using (var _connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT Id, pc_processdate, pc_nomcomred, pc_numdoc, motivo FROM dbo.commerce_quarantine";
                using (var _command = new SqlCommand(query, _connection))
                {
                    _command.CommandType = CommandType.Text;
                    _connection.Open();

                    using (var reader = _command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new CommerceQuarantine
                            {
                                Id = reader.GetInt32(0),
                                PcProcessDate = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                PcNomComRed = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                PcNumDoc = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                Motivo = reader.GetString(4)
                            });
                        }
                    }
                }
            }

            return list;
        }
    }
}
