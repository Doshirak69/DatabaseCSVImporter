using DatabaseCsvImporter.Model;
using Npgsql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCsvImporter.Service
{
    public class DatabaseService: IAsyncDisposable
    {
        private readonly string _connectionString;
        private NpgsqlDataSource? _connect;
        
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _departmentLocks = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _positionLocks = new();

        private readonly ConcurrentDictionary<string, Guid> _departmentCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, Guid> _positionCache = new(StringComparer.OrdinalIgnoreCase);

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Task OpenAsync()
        {
            _connect = NpgsqlDataSource.Create(_connectionString);
            return Task.CompletedTask;
        }

        public async Task<Guid> GetOrCreatePositionIdAsync(PositionModel position)
        {
            if (_connect is null) throw new InvalidOperationException("Нет подключения");

            var lockKey = _positionLocks.GetOrAdd(position.Name, _ => new SemaphoreSlim(1, 1));
            await lockKey.WaitAsync();
            try
            {
                if (_positionCache.TryGetValue(position.Name, out var cached))
                {
                    position.Id = cached;
                    return cached;
                }
                using var conn = await _connect.OpenConnectionAsync();
                using var cmd = new NpgsqlCommand("SELECT public.ddfn_get_or_create_position(@pos_name::text)", conn);
                cmd.Parameters.AddWithValue("pos_name", NpgsqlTypes.NpgsqlDbType.Text, position.Name);

                var obj = await cmd.ExecuteScalarAsync();
                var id = (Guid)(obj ?? throw new InvalidOperationException($"RowID должности не получен для '{position.Name}'"));

                position.Id = id;
                _positionCache[position.Name] = id;
                return id;
            }
            finally
            {
                lockKey.Release();
            }

        }
        public async Task<Guid> GetOrCreateDepartmentIdAsync(DepartmentModel department)
        {
            if (_connect is null) throw new InvalidOperationException("Нет подключения");

            var lockKey = _departmentLocks.GetOrAdd(department.Name, _ => new SemaphoreSlim(1, 1));
            await lockKey.WaitAsync();

            try
            {
                if (_departmentCache.TryGetValue(department.Name, out var cached))
                {
                    department.Id = cached;
                    return cached;
                }
                using var conn = await _connect.OpenConnectionAsync();
                using var cmd = new NpgsqlCommand("SELECT public.ddfn_get_or_create_subdivision(@sub_name::text)",
                     conn);

                cmd.Parameters.AddWithValue("sub_name", NpgsqlTypes.NpgsqlDbType.Text, department.Name);

                var obj = await cmd.ExecuteScalarAsync();
                var id = (Guid)(obj ?? throw new InvalidOperationException($"RowID подразделения не получен для '{department.Name}'"));

                department.Id = id;
                _departmentCache[department.Name] = id;
                return id;
            }
            finally
            {
                lockKey.Release();
            }
        }

        public async Task<Guid> GetOrCreateEmployeeIdAsync(EmployeeModel employee)
        {
            if (_connect is null) throw new InvalidOperationException("Нет подключения");

            using var conn = await _connect.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT public.ddfn_get_or_create_employee(@p_first_name::text, @p_middle_name::text, @p_last_name::text, @p_position_id::uuid, @p_parent_rowid::uuid)",
                conn);

            cmd.Parameters.AddWithValue("p_first_name", NpgsqlTypes.NpgsqlDbType.Text, employee.FirstName);
            cmd.Parameters.AddWithValue("p_middle_name", NpgsqlTypes.NpgsqlDbType.Text, employee.MiddleName);
            cmd.Parameters.AddWithValue("p_last_name", NpgsqlTypes.NpgsqlDbType.Text, employee.LastName);
            cmd.Parameters.AddWithValue("p_position_id", NpgsqlTypes.NpgsqlDbType.Uuid, employee.Position.Id);
            cmd.Parameters.AddWithValue("p_parent_rowid", NpgsqlTypes.NpgsqlDbType.Uuid, employee.Department.Id);

            var obj = await cmd.ExecuteScalarAsync();
            var id = (Guid)(obj ?? throw new InvalidOperationException("RowID сотрудника не получен"));

            employee.Id = id;
            return id;
        }

        public async Task UpsertCityStatusAsync(CityModel city, Guid employeeId)
        {
            if (_connect is null) throw new InvalidOperationException("Нет подключения");

            using var conn = await _connect.OpenConnectionAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT public.ddfn_upsert_city_status_by_city_name(@p_city_name::text, @p_employee_id::uuid, @p_status::boolean)",
                conn);

            cmd.Parameters.AddWithValue("p_city_name", NpgsqlTypes.NpgsqlDbType.Text, city.Name);
            cmd.Parameters.AddWithValue("p_employee_id", NpgsqlTypes.NpgsqlDbType.Uuid, employeeId);
            cmd.Parameters.AddWithValue("p_status", NpgsqlTypes.NpgsqlDbType.Boolean, city.Status);

            await cmd.ExecuteScalarAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_connect is not null) await _connect.DisposeAsync();

            foreach (var semaphore in _departmentLocks.Values)
                semaphore?.Dispose();
            foreach (var semaphore in _positionLocks.Values)
                semaphore?.Dispose();
        }
    }
}
