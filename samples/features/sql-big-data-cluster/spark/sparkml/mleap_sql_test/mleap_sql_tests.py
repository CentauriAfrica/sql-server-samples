import os
dir_path = os.path.dirname(os.path.realpath(__file__))

import sys
sys.path.append(os.path.join(dir_path, os.pardir, os.pardir, os.pardir))

from spark_submit import *

from subprocess import run, PIPE
import pytest
import pyodbc


@pytest.fixture(scope="module")
def setup_mod():
    print("setting up module ...")

    odbcDriver = "ODBC Driver 13 for SQL Server"
    databaseName = "tempdb"
    headNode = "master-0.master-svc"

    # Read sql username and password from environment variable.
    username = os.environ["EXTENSIBILITY_TEST_SQL_USER"]
    password = os.environ["EXTENSIBILITY_TEST_SQL_PASSWORD"]
    if not username or not password:
        raise Exception("Environment variable EXTENSIBILITY_TEST_SQL_USER or EXTENSIBILITY_TEST_SQL_PASSWORD cannot not be found")

    # enable SPEES
    conn = pyodbc.connect('DRIVER={0};SERVER={1};DATABASE={2};UID={3};PWD={4}'.format(
        odbcDriver, headNode, databaseName, username, password), autocommit=True)
    cursor = conn.cursor()
    cursor.execute("""EXEC sp_configure 'external scripts enabled', 1""")
    assert(-1 == cursor.rowcount)

    cursor.execute("""RECONFIGURE""")
    assert(-1 == cursor.rowcount)

    yield dict(cursor=cursor)

    print("tearing down module ...")


def test_java_spees(setup_mod):
    # exectue a Java SPEES query to create external libraries
    cursor = setup_mod['cursor']
    cursor.execute("""
        --SELECT @@SERVERNAME AS 'Server Name', @@VERSION AS 'Server Version', @@SERVICENAME AS 'Service Name'

        IF NOT EXISTS (SELECT * FROM sys.external_languages WHERE language = 'Java')
            --DROP EXTERNAL LANGUAGE Java;
            CREATE EXTERNAL LANGUAGE Java
                FROM (CONTENT = N'/opt/mssql/lib/extensibility/java-lang-extension.tar.gz', file_name = 'javaextension.so');

        IF EXISTS (SELECT * FROM sys.external_libraries WHERE name = 'SdkPackage')
            DROP EXTERNAL LIBRARY SdkPackage;
        CREATE EXTERNAL LIBRARY SdkPackage
            FROM (CONTENT = '/opt/mssql/lib/mssql-java-lang-extension.jar') WITH (LANGUAGE = 'Java');

        IF EXISTS (SELECT * FROM sys.external_libraries WHERE name = 'TestPackage')
            DROP EXTERNAL LIBRARY TestPackage
        CREATE EXTERNAL LIBRARY TestPackage
            FROM (CONTENT = '/opt/mssql/java/jars/JavaTestPackage.jar') WITH (LANGUAGE = 'Java');

        DECLARE @script NVARCHAR(max) = N'JavaTestPackage.PassThrough' --no space allowed in the string!
        EXEC sp_execute_external_script
            @language = N'Java'
            , @script = @script
            , @input_data_1 = N'SELECT 1'
        """)

    rows = cursor.fetchall()
    assert(1 == len(rows))
    assert(1 == rows[0][0])


def dictfetchall(cursor):
    '''fetch all rows from a cursor and return them as a dict'''
    colnames = [col[0] for col in cursor.description]
    return [dict(zip(colnames, row)) for row in cursor.fetchall()]


def test_mleap_pyspark(setup_mod):
    # train a pyspark model and export it as a mleap bundle
    hdfs_path = "/spark_ml"
    model_name_export = "adult_census_pipeline.zip"

    file_path = 'mleap_pyspark.py'
    file_args = [hdfs_path, model_name_export]
    ret = spark_submit(file_path, file_args)
    assert 0 == ret

    # get the mleap bundle from hdfs and copy it to the mssql-server container of the master-0 pod
    hdfs_file_path = os.path.join(hdfs_path, model_name_export)
    ret = run(["hdfs", "dfs", "-get", "-f", hdfs_file_path], stdout=PIPE, stderr=PIPE).returncode
    assert 0 == ret

    local_file_path = os.path.join("master-0:", "tmp")
    ret = run(["kubectl", "cp", model_name_export, local_file_path, "-c", "mssql-server"], stdout=PIPE, stderr=PIPE).returncode
    assert 0 == ret

    # exectue a Java SPEES query to serve the mleap bundle
    cursor = setup_mod['cursor']
    cursor.execute("""
        --suppresses the record count values generated by DML statements
        --like UPDATE and allows the result set to be retrieved directly.
        SET NOCOUNT ON;

        IF EXISTS (SELECT * FROM sys.external_libraries WHERE name = 'MleapApp')
            DROP EXTERNAL LIBRARY MleapApp;
        CREATE EXTERNAL LIBRARY MleapApp
            FROM (CONTENT = '/opt/mssql/java/jars/mssql-mleap-app-assembly-1.0.jar') WITH (LANGUAGE = 'Java')

        DROP TABLE IF EXISTS ##test
        CREATE TABLE ##test (
            income nvarchar(10)
            , age int
            , hours_per_week int
            , education nvarchar(10)
            , sex nvarchar(10)
            );
        INSERT INTO ##test values ('<=50K',	39,	40, 'Bachelors', 'Male');
        INSERT INTO ##test values ('<=50K',	50,	13, 'Bachelors', 'Male');
        INSERT INTO ##test values ('<=50K',	38,	40, 'HS-grad', 'Male');
        --SELECT * FROM ##test

        DECLARE @script NVARCHAR(max) = N'com.microsoft.sqlserver.mleap.Scorer' --no space allowed in the string!
        DECLARE @language nvarchar(4) = N'Java'
        DECLARE @parallel bit = 0
        DECLARE @input_data_1 nvarchar(97) = N'select age, hours_per_week, education, sex, income from ##test'
        DECLARE @params nvarchar(200) = N'@modelPath nvarchar(100), @outputFields nvarchar(100), @logLevel nvarchar(100)'
        DECLARE @modelPath nvarchar(100) = N'/tmp/adult_census_pipeline.zip'
        DECLARE @outputFields nvarchar(100) = N'prediction,probability,education,sex,income,predictedIncome'
        DECLARE @logLevel nvarchar(100) = N'INFO'
        EXEC sp_execute_external_script @language = @language, @script = @script, @parallel = @parallel
        , @input_data_1 = @input_data_1
        , @params = @params, @modelPath = @modelPath, @outputFields = @outputFields, @logLevel = @logLevel
        WITH RESULT SETS ((prediction int, probability0 float, probability1 float, education nvarchar(20), sex nvarchar(20), income nvarchar(20), predictedIncome nvarchar(20)))
        """)

    rows = dictfetchall(cursor)
    #pandas.DataFrame(rows)

    assert rows == [
        {'education': 'Bachelors',
        'income': '<=50K',
        'predictedIncome': '<=50K',
        'prediction': 0,
        'probability0': 0.6544871023375456,
        'probability1': 0.3455128976624544,
        'sex': 'Male'},
        {'education': 'Bachelors',
        'income': '<=50K',
        'predictedIncome': '<=50K',
        'prediction': 0,
        'probability0': 0.7363751868447964,
        'probability1': 0.2636248131552036,
        'sex': 'Male'},
        {'education': 'HS-grad',
        'income': '<=50K',
        'predictedIncome': '<=50K',
        'prediction': 0,
        'probability0': 0.8324466132959966,
        'probability1': 0.16755338670400344,
        'sex': 'Male'}]
