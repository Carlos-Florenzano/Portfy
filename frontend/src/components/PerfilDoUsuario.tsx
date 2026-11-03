// Cartão de perfil do cabeçalho + edição do nome. O salário não é editado aqui:
// ele pertence ao módulo "Salário e Orçamento", que tem rota dedicada.
import { useState } from 'react';
import { Check, Pencil, X } from 'lucide-react';
import { mensagemDeErro } from '../services/api';
import { usuarioService } from '../services/UsuarioService';
import type { Usuario } from '../services/UsuarioService';
import { Aviso, Botao } from './ui';
import { classesDeEntrada, classesDeRotulo } from './formatadores';

export function PerfilDoUsuario({
    usuario,
    onAtualizado,
}: {
    usuario: Usuario;
    onAtualizado: (usuario: Usuario) => void;
}) {
    const [editando, setEditando] = useState(false);
    const [nome, setNome] = useState(usuario.nome);
    const [salvando, setSalvando] = useState(false);
    const [erro, setErro] = useState('');

    function cancelar() {
        setNome(usuario.nome);
        setErro('');
        setEditando(false);
    }

    async function salvar() {
        try {
            setSalvando(true);
            setErro('');

            // A rota de perfil recebe nome e salário; o salário vigente é reenviado
            // sem alteração para não zerar a renda cadastrada.
            await usuarioService.atualizar(usuario.id, {
                nome: nome.trim(),
                salarioMensal: usuario.salarioMensal,
            });

            onAtualizado({ ...usuario, nome: nome.trim() });
            setEditando(false);
        } catch (falha: unknown) {
            setErro(mensagemDeErro(falha));
        } finally {
            setSalvando(false);
        }
    }

    return (
        <div className="w-full max-w-sm">
            <div className="flex items-center gap-3 rounded-2xl border border-portfy-borda bg-portfy-superficie px-4 py-3">
                <span className="flex size-11 shrink-0 items-center justify-center rounded-full bg-portfy-acento text-lg font-bold text-white">
                    {usuario.nome ? usuario.nome.charAt(0).toUpperCase() : 'U'}
                </span>

                <span className="min-w-0 flex-1">
                    <strong className="block truncate text-sm text-portfy-texto">{usuario.nome}</strong>
                    <small className="block truncate text-xs text-portfy-suave">{usuario.login}</small>
                </span>

                <button
                    type="button"
                    onClick={() => (editando ? cancelar() : setEditando(true))}
                    className="flex items-center gap-1.5 rounded-lg px-2.5 py-1.5 text-xs font-semibold text-portfy-acento-claro transition hover:bg-portfy-acento/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-portfy-acento/40"
                >
                    <Pencil size={14} aria-hidden />
                    {editando ? 'Cancelar' : 'Editar perfil'}
                </button>
            </div>

            {editando && (
                <div className="mt-3 rounded-2xl border border-portfy-borda bg-portfy-superficie p-4">
                    <label className={classesDeRotulo} htmlFor="perfil-nome">
                        Nome completo
                    </label>
                    <input
                        id="perfil-nome"
                        className={classesDeEntrada}
                        value={nome}
                        onChange={(evento) => setNome(evento.target.value)}
                        placeholder="Seu nome"
                    />

                    {erro !== '' && (
                        <div className="mt-3">
                            <Aviso tipo="erro" texto={erro} />
                        </div>
                    )}

                    <div className="mt-3 flex gap-2">
                        <Botao onClick={() => void salvar()} disabled={salvando || nome.trim() === ''}>
                            <Check size={16} aria-hidden />
                            {salvando ? 'Salvando...' : 'Salvar'}
                        </Botao>
                        <Botao variante="secundario" onClick={cancelar} disabled={salvando}>
                            <X size={16} aria-hidden />
                            Cancelar
                        </Botao>
                    </div>
                </div>
            )}
        </div>
    );
}
