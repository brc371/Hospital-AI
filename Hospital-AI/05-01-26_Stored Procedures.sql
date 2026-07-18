/* #########################################################
			Patients with critical labs
Description: Gets list of patients with labs flagged as critical
Input(s): p_patient_id
Call example(s): 
- CALL GetPatientsWithCriticalLabs(NULL); Returns all patients with critical labs
- CALL GetPatientsWithCriticalLabs(10); Returns all critical labs for this patient
- CALL GetPatientsWithCriticalLabs(99999); Tests error for non-existant patients

*/
DROP PROCEDURE IF EXISTS GetPatientsWithCriticalLabs;

DELIMITER $$
CREATE PROCEDURE GetPatientsWithCriticalLabs(
    IN p_patient_id INT
)
BEGIN
    DECLARE v_patient_count INT DEFAULT 0;
    DECLARE v_critical_count INT DEFAULT 0;

    -- General SQL error handler
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        RESIGNAL;
    END;

    -- Validate patient_id only if one was provided
    IF p_patient_id IS NOT NULL THEN

        SELECT COUNT(*)
        INTO v_patient_count
        FROM Patients
        WHERE patient_id = p_patient_id;

        IF v_patient_count = 0 THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Patient does not exist.';
        END IF;

    END IF;

    -- Check whether critical labs exist for the requested scope
    SELECT COUNT(*)
    INTO v_critical_count
    FROM LabResults lr
    WHERE lr.critical_flag = 1
      AND (
            p_patient_id IS NULL
            OR lr.patient_id = p_patient_id
          );

    -- If no critical labs exist, return a friendly message
    IF v_critical_count = 0 THEN

        SELECT
            'No critical lab results found.' AS message;

    ELSE

        SELECT
            p.patient_id,
            p.first_name,
            p.last_name,
            lr.lab_result_id,
            lr.admission_id,
            lr.test_name,
            lr.result_value,
            lr.unit,
            lr.reference_range,
            lr.date_collected,
            lr.critical_flag
        FROM LabResults lr
        INNER JOIN Patients p
            ON lr.patient_id = p.patient_id
        WHERE lr.critical_flag = 1
          AND (
                p_patient_id IS NULL
                OR lr.patient_id = p_patient_id
              )
        ORDER BY lr.date_collected DESC, p.last_name, p.first_name;

    END IF;
END $$

DELIMITER ;

CALL GetPatientsWithCriticalLabs(5);
/* #########################################################
			Patients Summary
Description: Gets quick summary of patient hospitalizations. 
Returns three sets of data: 
1) demographics + medical history
2) Medication history
3) Admissions history
*NOTE: If using mysql, the results will show in three different
output tabs in the result grid. 

Input(s): patient_id
Call example(s): 
- CALL GetPatientSummary(40);
*/
DROP PROCEDURE IF EXISTS GetPatientSummary;

DELIMITER $$
CREATE PROCEDURE GetPatientSummary(
    IN p_patient_id INT
)
BEGIN
    DECLARE v_patient_count INT DEFAULT 0;
    DECLARE v_medication_count INT DEFAULT 0;
    DECLARE v_admission_count INT DEFAULT 0;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        RESIGNAL;
    END;

    -- Validate input
    IF p_patient_id IS NULL OR p_patient_id <= 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Invalid patient ID. Please provide a valid positive patient_id.';
    END IF;

    -- Check whether patient exists
    SELECT COUNT(*)
    INTO v_patient_count
    FROM Patients
    WHERE patient_id = p_patient_id;

    IF v_patient_count = 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Patient does not exist.';
    END IF;

    -- Count related medications
    SELECT COUNT(*)
    INTO v_medication_count
    FROM Medications
    WHERE patient_id = p_patient_id;

    -- Count related admissions
    SELECT COUNT(*)
    INTO v_admission_count
    FROM Admissions
    WHERE patient_id = p_patient_id;

    -- Result Set 1: Patient demographics + medical history
    SELECT
        'Patient Demographics and Medical History' AS result_section,
        p.patient_id,
        p.first_name,
        p.last_name,
        p.date_of_birth,
        p.gender,
        p.phone_number,
        p.emergency_contact_name,
        p.emergency_contact_phone,
        mh.chronic_conditions,
        mh.prior_surgeries,
        mh.allergies,
        mh.family_history,
        mh.notes
    FROM Patients p
    LEFT JOIN MedicalHistory mh
        ON p.patient_id = mh.patient_id
    WHERE p.patient_id = p_patient_id;

    -- Result Set 2: Patient medications or message
    IF v_medication_count = 0 THEN
        SELECT
            'Patient Medications' AS result_section,
            p_patient_id AS patient_id,
            'No medication records found for this patient.' AS message;
    ELSE
        SELECT
            'Patient Medications' AS result_section,
            p.patient_id,
            p.first_name,
            p.last_name,
            m.medication_id,
            m.drug_name,
            m.dose,
            m.frequency,
            m.start_date,
            m.end_date,
            ph.first_name AS prescribing_physician_first_name,
            ph.last_name AS prescribing_physician_last_name
        FROM Medications m
        INNER JOIN Patients p
            ON m.patient_id = p.patient_id
        LEFT JOIN Physicians ph
            ON m.prescribing_physician_id = ph.physician_id
        WHERE m.patient_id = p_patient_id
        ORDER BY m.start_date DESC;

    END IF;

    -- Result Set 3: Patient admissions or message
    IF v_admission_count = 0 THEN
        SELECT
            'Patient Admissions' AS result_section,
            p_patient_id AS patient_id,
            'No admission records found for this patient.' AS message;
    ELSE
        SELECT
            'Patient Admissions' AS result_section,
            p.patient_id,
            p.first_name,
            p.last_name,
            a.admission_id,
            a.admission_date,
            a.discharge_date,
            a.department,
            a.bed_number,
            a.reason_for_visit,
            ph.first_name AS attending_physician_first_name,
            ph.last_name AS attending_physician_last_name
        FROM Admissions a
        INNER JOIN Patients p
            ON a.patient_id = p.patient_id
        LEFT JOIN Physicians ph
            ON a.attending_physician_id = ph.physician_id
        WHERE a.patient_id = p_patient_id
        ORDER BY a.admission_date DESC;

    END IF;

END $$
DELIMITER ;

CALL GetPatientSummary();
/* #########################################################
					Get lab history for one patient
Description: Gets lab history for a given patient
Input(s): patient_id
Call exmaple: CALL GetPatientLabHistory(5);
*/
DROP PROCEDURE IF EXISTS GetPatientLabHistory;

DELIMITER $$
CREATE PROCEDURE GetPatientLabHistory(
    IN p_patient_id INT
)
BEGIN
    DECLARE v_patient_count INT DEFAULT 0;
    DECLARE v_lab_count INT DEFAULT 0;

    SELECT COUNT(*)
    INTO v_patient_count
    FROM Patients
    WHERE patient_id = p_patient_id;

    IF v_patient_count = 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Patient does not exist.';
    END IF;

    SELECT COUNT(*)
    INTO v_lab_count
    FROM LabResults
    WHERE patient_id = p_patient_id;

    IF v_lab_count = 0 THEN
        SELECT
            p_patient_id AS patient_id,
            'No lab results found for this patient.' AS message;
    ELSE
        SELECT
            p.patient_id,
            p.first_name,
            p.last_name,
            lr.lab_result_id,
            lr.admission_id,
            lr.test_name,
            lr.result_value,
            lr.unit,
            lr.reference_range,
            lr.date_collected,
            lr.critical_flag
        FROM LabResults lr
        INNER JOIN Patients p
            ON lr.patient_id = p.patient_id
        WHERE lr.patient_id = p_patient_id
        ORDER BY lr.date_collected DESC, lr.test_name;
    END IF;
END $$

DELIMITER ;

CALL GetPatientLabHistory(11);

/* #########################################################
					Get currently admitted patients
Description: This assumes patients are currently admitted when discharge_date IS NULL.
This also does a quick data integrity check to see if all admissions reference 
current patients in database. 
Input(s): None
Call example: CALL GetCurrentlyAdmittedPatients();
*/

DROP PROCEDURE IF EXISTS GetCurrentlyAdmittedPatients;

DELIMITER $$
CREATE PROCEDURE GetCurrentlyAdmittedPatients()
BEGIN
    DECLARE v_current_admission_count INT DEFAULT 0;
    DECLARE v_orphan_admission_count INT DEFAULT 0;

    -- General SQL error handler
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        RESIGNAL;
    END;

    -- Check for admissions records whose patient_id does not exist in Patients
    SELECT COUNT(*)
    INTO v_orphan_admission_count
    FROM Admissions a
    LEFT JOIN Patients p
        ON a.patient_id = p.patient_id
    WHERE p.patient_id IS NULL;

    IF v_orphan_admission_count > 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Data integrity error: One or more admissions reference a patient that does not exist.';
    END IF;

    -- Count currently admitted patients
    SELECT COUNT(*)
    INTO v_current_admission_count
    FROM Admissions
    WHERE discharge_date IS NULL;

    -- If no current admissions exist, return a friendly message
    IF v_current_admission_count = 0 THEN

        SELECT
            'No currently admitted patients found.' AS message;

    ELSE

        SELECT
            a.admission_id,
            p.patient_id,
            p.first_name,
            p.last_name,
            a.admission_date,
            a.department,
            a.bed_number,
            a.reason_for_visit,
            ph.first_name AS attending_physician_first_name,
            ph.last_name AS attending_physician_last_name
        FROM Admissions a
        INNER JOIN Patients p
            ON a.patient_id = p.patient_id
        LEFT JOIN Physicians ph
            ON a.attending_physician_id = ph.physician_id
        WHERE a.discharge_date IS NULL
        ORDER BY a.department, a.bed_number;

    END IF;

END $$
DELIMITER ;

CALL GetCurrentlyAdmittedPatients();
/* #########################################################
					Get patients by physician
Description: List of patients attended by a given physician.
Input(s): physician_id
Call example: CALL GetPatientsByPhysician(10);
*/
DROP PROCEDURE IF EXISTS GetPatientsByPhysician;

DELIMITER $$
CREATE PROCEDURE GetPatientsByPhysician(
    IN p_physician_id INT
)
BEGIN
    DECLARE v_physician_count INT DEFAULT 0;
    DECLARE v_patient_count INT DEFAULT 0;

    -- General SQL error handler
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        RESIGNAL;
    END;

    -- Validate input
    IF p_physician_id IS NULL OR p_physician_id <= 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Invalid physician ID. Please provide a valid positive physician_id.';
    END IF;

    -- Check whether physician exists
    SELECT COUNT(*)
    INTO v_physician_count
    FROM Physicians
    WHERE physician_id = p_physician_id;

    IF v_physician_count = 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Physician does not exist.';
    END IF;

    -- Check whether physician has assigned admissions/patients
    SELECT COUNT(*)
    INTO v_patient_count
    FROM Admissions
    WHERE attending_physician_id = p_physician_id;

    IF v_patient_count = 0 THEN

        SELECT
            p_physician_id AS physician_id,
            'No patients/admissions found for this physician.' AS message;

    ELSE

        SELECT DISTINCT
            ph.physician_id,
            ph.first_name AS physician_first_name,
            ph.last_name AS physician_last_name,
            p.patient_id,
            p.first_name AS patient_first_name,
            p.last_name AS patient_last_name,
            p.date_of_birth,
            p.gender,
            a.admission_id,
            a.admission_date,
            a.discharge_date,
            a.department,
            a.reason_for_visit
        FROM Admissions a
        INNER JOIN Patients p
            ON a.patient_id = p.patient_id
        INNER JOIN Physicians ph
            ON a.attending_physician_id = ph.physician_id
        WHERE a.attending_physician_id = p_physician_id
        ORDER BY p.last_name, p.first_name;

    END IF;
END $$
DELIMITER ;

CALL GetPatientsByPhysician(100);
/* #########################################################
					Search patient by name
Description: Return patient details. Usefull to see if patient exists in DB.
Input(s): Patient name
Call example:
- CALL SearchPatientsByName('Maurice');
*/
DROP PROCEDURE IF EXISTS SearchPatientsByName;

DELIMITER $$
CREATE PROCEDURE SearchPatientsByName(
    IN p_search_text VARCHAR(100)
)
BEGIN
    DECLARE v_search_text VARCHAR(100);
    DECLARE v_result_count INT DEFAULT 0;

    -- General SQL error handler
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        RESIGNAL;
    END;

    -- Clean input
    SET v_search_text = TRIM(p_search_text);

    -- Validate input
    IF v_search_text IS NULL OR v_search_text = '' THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Search text cannot be empty. Please enter a first name, last name, or full name.';
    END IF;

    -- Prevent searches that are too broad
    IF CHAR_LENGTH(v_search_text) < 2 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Search text must be at least 2 characters long.';
    END IF;

    -- Count matching patients
    SELECT COUNT(*)
    INTO v_result_count
    FROM Patients
    WHERE first_name LIKE CONCAT('%', v_search_text, '%')
       OR last_name LIKE CONCAT('%', v_search_text, '%')
       OR CONCAT(first_name, ' ', last_name) LIKE CONCAT('%', v_search_text, '%');

    -- Return friendly message if no patients found
    IF v_result_count = 0 THEN
        SELECT
            v_search_text AS search_text,
            'No patients found matching this search.' AS message;
    ELSE
        SELECT
            patient_id,
            first_name,
            last_name,
            date_of_birth,
            gender,
            phone_number
        FROM Patients
        WHERE first_name LIKE CONCAT('%', v_search_text, '%')
           OR last_name LIKE CONCAT('%', v_search_text, '%')
           OR CONCAT(first_name, ' ', last_name) LIKE CONCAT('%', v_search_text, '%')
        ORDER BY last_name, first_name;

    END IF;
END $$
DELIMITER ;

CALL SearchPatientsByName('Maurice');
