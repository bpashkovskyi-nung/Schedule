(function () {
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
                const params = new URLSearchParams({
                    facultyName: faculty.value || '',
                    course: course ? course.value : '0',
                });
                window.location = '/Home/ReloadUnifiedLists?' + params.toString();
            }
            faculty.addEventListener('change', reloadUnified);
            if (course) course.addEventListener('change', reloadUnified);
        }
    });
})();
