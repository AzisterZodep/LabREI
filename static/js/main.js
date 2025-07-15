document.addEventListener('DOMContentLoaded', () => {
    const pecasListDiv = document.getElementById('pecas-list');
    const computadoresListDiv = document.getElementById('computadores-list');

    async function fetchPecas() {
        try {
            const response = await fetch('/pecas');
            const pecas = await response.json();
            pecasListDiv.innerHTML += pecas.map(peca => `
                <p>ID: ${peca.id}, Nome: ${peca.nome}, Tipo: ${peca.tipo}, Estado: ${peca.estado}</p>
            `).join('');
        } catch (error) {
            console.error('Erro ao buscar peças:', error);
            pecasListDiv.innerHTML += '<p style="color: red;">Erro ao carregar peças.</p>';
        }
    }

    async function fetchComputadores() {
        try {
            const response = await fetch('/computadores');
            const computadores = await response.json();
            computadoresListDiv.innerHTML += computadores.map(pc => `
                <p>ID: ${pc.id}, Nome: ${pc.nome}, Estado: ${pc.estado}, Peças: ${pc.pecas.length}</p>
            `).join('');
        } catch (error) {
            console.error('Erro ao buscar computadores:', error);
            computadoresListDiv.innerHTML += '<p style="color: red;">Erro ao carregar computadores.</p>';
        }
    }

    fetchPecas();
    fetchComputadores();
});