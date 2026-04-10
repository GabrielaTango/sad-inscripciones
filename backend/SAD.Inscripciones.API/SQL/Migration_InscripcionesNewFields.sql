-- Migration: Add new fields to Inscripciones table
-- Date: 2026-02-21
-- Description: Adds personal, address, professional, and workplace fields

ALTER TABLE Inscripciones ADD
    FechaNacimiento DATE NULL,
    Domicilio NVARCHAR(200) NULL,
    CodigoPostal NVARCHAR(20) NULL,
    Localidad NVARCHAR(100) NULL,
    Pais NVARCHAR(100) NULL,
    Celular NVARCHAR(50) NULL,
    Profesion NVARCHAR(100) NULL,
    Especialidad NVARCHAR(100) NULL,
    Institucion NVARCHAR(200) NULL,
    Sector NVARCHAR(200) NULL;
