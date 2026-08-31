(function () {
    const stateStorageKey = 'schedule.legacy.form-state.v1';

    function getForm(formId) {
        return document.getElementById(formId);
    }

    function getField(form, name) {
        return form?.elements.namedItem(name);
    }

    function getValue(form, name) {
        return getField(form, name)?.value || null;
    }

    function getChecked(form, name) {
        return Boolean(getField(form, name)?.checked);
    }

    function getDateFormState(formId) {
        const form = getForm(formId);
        return {
            periodKey: getValue(form, 'PeriodKey') || 'to_end_of_week',
            fromDate: getValue(form, 'FromDate'),
            toDate: getValue(form, 'ToDate'),
            weeklyView: getChecked(form, 'WeeklyView'),
        };
    }

    function getCurrentTab() {
        const activeButton = document.querySelector('.nav-link.active[data-bs-target]');
        return activeButton?.getAttribute('data-bs-target')?.substring(1) ||
            document.getElementById('activeTab')?.value ||
            'main-schedule';
    }

    function buildClientState(activeTab) {
        const unified = getDateFormState('unifiedForm');
        unified.facultyName = getValue(getForm('unifiedForm'), 'FacultyName');
        unified.course = Number.parseInt(getValue(getForm('unifiedForm'), 'Course') || '0', 10) || 0;
        unified.teacherId = getValue(getForm('unifiedForm'), 'TeacherId');
        unified.groupId = getValue(getForm('unifiedForm'), 'GroupId');

        const rooms = getDateFormState('roomsForm');
        rooms.blockName = getValue(getForm('roomsForm'), 'BlockName');
        rooms.roomId = getValue(getForm('roomsForm'), 'RoomId');

        const teachers = getDateFormState('teachersForm');
        teachers.departmentName = getValue(getForm('teachersForm'), 'DepartmentName');
        teachers.teacherId = getValue(getForm('teachersForm'), 'TeacherId');

        const groups = getDateFormState('groupsForm');
        groups.departmentName = getValue(getForm('groupsForm'), 'DepartmentName');
        groups.groupId = getValue(getForm('groupsForm'), 'GroupId');

        const teachingLoad = getDateFormState('teachingLoadForm');
        teachingLoad.departmentName = getValue(getForm('teachingLoadForm'), 'DepartmentName');

        const auditoriumLoad = getDateFormState('auditoriumsForm');
        auditoriumLoad.blockName = getValue(getForm('auditoriumsForm'), 'BlockName');
        auditoriumLoad.pairsFrom = Number.parseInt(getValue(getForm('auditoriumsForm'), 'PairsFrom') || '1', 10) || 1;
        auditoriumLoad.pairsTo = Number.parseInt(getValue(getForm('auditoriumsForm'), 'PairsTo') || '8', 10) || 8;

        return {
            activeTab: activeTab || getCurrentTab(),
            unified,
            rooms,
            teachers,
            groups,
            teachingLoad,
            auditoriumLoad,
        };
    }

    function loadClientState() {
        try {
            const serialized = sessionStorage.getItem(stateStorageKey);
            return serialized ? JSON.parse(serialized) : null;
        } catch (_) {
            return null;
        }
    }

    function saveClientState(activeTab) {
        try {
            sessionStorage.setItem(stateStorageKey, JSON.stringify(buildClientState(activeTab)));
        } catch (_) {
            // Session storage can be unavailable when browser privacy settings block it.
        }
    }

    function setValue(form, name, value) {
        const field = getField(form, name);
        if (!field || value === undefined || value === null) return;
        if (field.type === 'checkbox') {
            field.checked = Boolean(value);
        } else {
            field.value = value;
        }
    }

    function restoreDateFormState(formId, state) {
        if (!state) return;
        const form = getForm(formId);
        setValue(form, 'PeriodKey', state.periodKey);
        setValue(form, 'FromDate', state.fromDate);
        setValue(form, 'ToDate', state.toDate);
        setValue(form, 'WeeklyView', state.weeklyView);
    }

    function restoreClientState(state) {
        if (!state) return;

        const unifiedForm = getForm('unifiedForm');
        restoreDateFormState('unifiedForm', state.unified);
        setValue(unifiedForm, 'FacultyName', state.unified?.facultyName);
        setValue(unifiedForm, 'Course', state.unified?.course);
        setValue(unifiedForm, 'TeacherId', state.unified?.teacherId);
        setValue(unifiedForm, 'GroupId', state.unified?.groupId);

        const roomsForm = getForm('roomsForm');
        restoreDateFormState('roomsForm', state.rooms);
        setValue(roomsForm, 'BlockName', state.rooms?.blockName);
        setValue(roomsForm, 'RoomId', state.rooms?.roomId);

        const teachersForm = getForm('teachersForm');
        restoreDateFormState('teachersForm', state.teachers);
        setValue(teachersForm, 'DepartmentName', state.teachers?.departmentName);
        setValue(teachersForm, 'TeacherId', state.teachers?.teacherId);

        const groupsForm = getForm('groupsForm');
        restoreDateFormState('groupsForm', state.groups);
        setValue(groupsForm, 'DepartmentName', state.groups?.departmentName);
        setValue(groupsForm, 'GroupId', state.groups?.groupId);

        const teachingLoadForm = getForm('teachingLoadForm');
        restoreDateFormState('teachingLoadForm', state.teachingLoad);
        setValue(teachingLoadForm, 'DepartmentName', state.teachingLoad?.departmentName);

        const auditoriumLoadForm = getForm('auditoriumsForm');
        restoreDateFormState('auditoriumsForm', state.auditoriumLoad);
        setValue(auditoriumLoadForm, 'BlockName', state.auditoriumLoad?.blockName);
        setValue(auditoriumLoadForm, 'PairsFrom', state.auditoriumLoad?.pairsFrom);
        setValue(auditoriumLoadForm, 'PairsTo', state.auditoriumLoad?.pairsTo);
    }

    function initPeriodFields(formId, periodId, fromId, toId) {
        const form = document.getElementById(formId);
        if (!form) return;

        const period = document.getElementById(periodId);
        const from = document.getElementById(fromId);
        const to = document.getElementById(toId);
        if (!period || !from || !to) return;

        const options = Array.from(period.options);
        function syncDates() {
            const selected = options.find(o => o.value === period.value);
            if (!selected) return;
            if (period.value !== 'custom') {
                from.value = selected.dataset.from || '';
                to.value = selected.dataset.to || '';
            }
            const custom = period.value === 'custom';
            from.disabled = !custom;
            to.disabled = !custom;
        }

        period.addEventListener('change', syncDates);
        syncDates();

        form.addEventListener('submit', function () {
            from.disabled = false;
            to.disabled = false;
        });
    }

    function initCascade(blockId, roomId, roomsJsonId) {
        const block = document.getElementById(blockId);
        const room = document.getElementById(roomId);
        const dataEl = document.getElementById(roomsJsonId);
        if (!block || !room || !dataEl) return;

        const roomsByBlock = JSON.parse(dataEl.textContent || '{}');
        function updateRooms() {
            const blockName = block.value;
            const selected = room.value;
            room.innerHTML = '<option value="">Оберіть аудиторію</option>';
            room.disabled = !blockName;
            if (!blockName) return;

            (roomsByBlock[blockName] || []).forEach(function (r) {
                const opt = document.createElement('option');
                opt.value = r.id;
                opt.textContent = r.name;
                if (r.id === selected) opt.selected = true;
                room.appendChild(opt);
            });
        }

        block.addEventListener('change', updateRooms);
        updateRooms();
    }

    function initDepartmentCascade(deptId, entityId, entitiesJsonId, placeholder) {
        const dept = document.getElementById(deptId);
        const entity = document.getElementById(entityId);
        const dataEl = document.getElementById(entitiesJsonId);
        if (!dept || !entity || !dataEl) return;

        const byDept = JSON.parse(dataEl.textContent || '{}');
        function update() {
            const name = dept.value;
            const selected = entity.value;
            entity.innerHTML = `<option value="">${placeholder}</option>`;
            entity.disabled = !name;
            if (!name) return;
            (byDept[name] || []).forEach(function (e) {
                const opt = document.createElement('option');
                opt.value = e.id;
                opt.textContent = e.name;
                if (e.id === selected) opt.selected = true;
                entity.appendChild(opt);
            });
        }

        dept.addEventListener('change', update);
        update();
    }

    document.addEventListener('DOMContentLoaded', function () {
        const savedState = loadClientState();
        restoreClientState(savedState);

        const activeTab = savedState?.activeTab || document.getElementById('activeTab')?.value;
        if (activeTab) {
            const tabBtn = document.querySelector(`[data-bs-target="#${activeTab}"]`);
            if (tabBtn) {
                new bootstrap.Tab(tabBtn).show();
            }
        }

        document.querySelectorAll('[data-bs-toggle="tab"]').forEach(function (tabButton) {
            tabButton.addEventListener('shown.bs.tab', function (event) {
                const target = event.target.getAttribute('data-bs-target');
                saveClientState(target ? target.substring(1) : getCurrentTab());
            });
        });

        document.querySelectorAll('form').forEach(function (form) {
            form.addEventListener('change', function () {
                saveClientState(getCurrentTab());
            });
            form.addEventListener('submit', function () {
                saveClientState(getCurrentTab());
            });
        });

        initPeriodFields('unifiedForm', 'unifiedPeriod', 'unifiedFrom', 'unifiedTo');
        initPeriodFields('roomsForm', 'roomsPeriod', 'roomsFrom', 'roomsTo');
        initPeriodFields('teachersForm', 'teachersPeriod', 'teachersFrom', 'teachersTo');
        initPeriodFields('groupsForm', 'groupsPeriod', 'groupsFrom', 'groupsTo');
        initPeriodFields('teachingLoadForm', 'teachingLoadPeriod', 'teachingLoadFrom', 'teachingLoadTo');
        initPeriodFields('auditoriumsForm', 'auditoriumsPeriod', 'auditoriumsFrom', 'auditoriumsTo');

        initCascade('roomsBlock', 'roomsRoom', 'roomsByBlockJson');
        initDepartmentCascade('teachersDept', 'teachersTeacher', 'teachersByDeptJson', 'Оберіть викладача');
        initDepartmentCascade('groupsDept', 'groupsGroup', 'groupsByDeptJson', 'Оберіть групу');
        const faculty = document.getElementById('unifiedFaculty');
        const course = document.getElementById('unifiedCourse');
        if (faculty) {
            function reloadUnified() {
                const teacher = document.getElementById('unifiedTeacher');
                const group = document.getElementById('unifiedGroup');
                if (teacher) teacher.value = '';
                if (group) group.value = '';
                saveClientState(getCurrentTab());
                const params = new URLSearchParams({
                    facultyName: faculty.value || '',
                    course: course ? course.value : '0',
                });
                window.location = '/Home/ReloadUnifiedLists?' + params.toString();
            }
            faculty.addEventListener('change', reloadUnified);
            if (course) course.addEventListener('change', reloadUnified);

            const savedFaculty = savedState?.unified?.facultyName;
            const teacherOptions = document.getElementById('unifiedTeacher')?.options.length || 0;
            const groupOptions = document.getElementById('unifiedGroup')?.options.length || 0;
            if (savedFaculty && faculty.value === savedFaculty && teacherOptions <= 1 && groupOptions <= 1) {
                const params = new URLSearchParams({
                    facultyName: faculty.value,
                    course: document.getElementById('unifiedCourse')?.value || '0',
                });
                window.location = '/Home/ReloadUnifiedLists?' + params.toString();
            }
        }
    });
})();
