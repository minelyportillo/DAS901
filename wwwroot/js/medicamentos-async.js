async function cargarMedicamentosAsync() {
  const tablaBody = document.getElementById("tablaMedicamentosBody");
  if (!tablaBody) return;

  try {
    const respuesta = await fetch("/api/medicamentos", {
      method: "GET",
      headers: { Accept: "application/json" }
    });

    if (!respuesta.ok) {
      throw new Error(`Error HTTP: ${respuesta.status}`);
    }

    const medicamentos = await respuesta.json();
    tablaBody.innerHTML = "";

    medicamentos.forEach((med) => {
      const fila = document.createElement("tr");

      const celdaNombre = document.createElement("td");
      celdaNombre.textContent = med.nombre ?? med.Nombre ?? "";

      const celdaStock = document.createElement("td");
      celdaStock.textContent = med.stock ?? med.cantidad ?? med.Stock ?? "";

      fila.appendChild(celdaNombre);
      fila.appendChild(celdaStock);
      tablaBody.appendChild(fila);
    });
  } catch (error) {
    console.error("Error al consultar medicamentos:", error);
  }
}

document.addEventListener("DOMContentLoaded", cargarMedicamentosAsync);
