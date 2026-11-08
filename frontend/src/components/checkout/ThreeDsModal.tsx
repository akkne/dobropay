import React from 'react';
import { Modal } from '../ui/Modal';
import { ThreeDsSmsChallenge } from './ThreeDsSmsChallenge';

export interface ThreeDsModalProps {
  isOpen: boolean;
  onSuccess: () => void;
  onCancel: () => void;
}

export const ThreeDsModal: React.FC<ThreeDsModalProps> = ({
  isOpen,
  onSuccess,
  onCancel
}) => {
  return (
    <Modal isOpen={isOpen} onClose={onCancel} maxWidth="420px">
      <ThreeDsSmsChallenge onSuccess={onSuccess} onCancel={onCancel} />
    </Modal>
  );
};
