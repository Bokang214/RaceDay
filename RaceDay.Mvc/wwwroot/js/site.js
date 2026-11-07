// RaceDay: tiny progressive-enhancement helpers. The site still works without JavaScript.
(function () {
    "use strict";

    // Ask before destructive actions: <form class="confirm-form" data-confirm="Are you sure?">
    document.querySelectorAll("form.confirm-form").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            var message = form.getAttribute("data-confirm") || "Are you sure?";
            if (!window.confirm(message)) {
                event.preventDefault();
            }
        });
    });

    // Submit a filter form as soon as its select changes.
    document.querySelectorAll("select[data-autosubmit]").forEach(function (select) {
        select.addEventListener("change", function () {
            if (select.form) {
                select.form.submit();
            }
        });
    });

    // Live preview when choosing an image: <input type="file" data-preview-target="elementId">
    document.querySelectorAll("input[type=file][data-preview-target]").forEach(function (input) {
        input.addEventListener("change", function () {
            var target = document.getElementById(input.getAttribute("data-preview-target"));
            var file = input.files && input.files[0];
            if (!target || !file || !file.type.startsWith("image/")) {
                return;
            }

            var img = target.querySelector("img");
            if (!img) {
                target.innerHTML = "";
                img = document.createElement("img");
                img.alt = "Selected banner preview";
                target.appendChild(img);
            }

            img.src = URL.createObjectURL(file);
        });
    });

    // Registration: only Participants have a date of birth.
    var dobGroup = document.getElementById("dobGroup");
    var roleInputs = document.querySelectorAll("input[name=Role]");
    if (dobGroup && roleInputs.length) {
        var update = function () {
            var selected = document.querySelector("input[name=Role]:checked");
            dobGroup.style.display = selected && selected.value === "Organiser" ? "none" : "";
        };
        roleInputs.forEach(function (r) { r.addEventListener("change", update); });
        update();
    }
})();
